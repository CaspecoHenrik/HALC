using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Api.Models.Extensions;

namespace Se.Caspeco.CloneDoc
{
    public class GridDocumentRenderer
    {
        public class IntermediatePageCell
        {
            public GridDocumentRow Row;
            public GridDocumentCell Cell;
            public double X;
            public double Y;
            public double Width;
            public double Height;

            public IntermediatePageCell(GridDocumentRow row, GridDocumentCell cell, double x, double y, double width, double height)
            {
                Row = row;
                Cell = cell;
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
        }

        public class IntermediatePage
        {
            public List<List<IntermediatePageCell>> PageCells;

            public int ColumnCount
            {
                get { return PageCells[0].Count; }
            }

            public int RowCount
            {
                get { return PageCells.Count; }
            }

            public IntermediatePage()
            {
                PageCells = new List<List<IntermediatePageCell>>();
            }
        }

        private int currentRowOffset = 0;
        private int currentColumnOffset = 0;
        private GridDocument _grid;
        private Document _document;
        private RenderParameters _parameters;
        private DocumentPage _documentPageForMetrics;

        /// <summary>
        /// Default constructor needed for (de)serialization.
        /// </summary>
        public GridDocumentRenderer()
        {
        }

        public GridDocumentRenderer(GridDocument grid, Document document, RenderParameters parameters)
        {
            _grid = grid;
            _document = document;
            _parameters = parameters;

            // create a page so that we can get the dimensions. this page will be cleared from the document
            _documentPageForMetrics = _document.AddPage(_parameters.PageType, _parameters.PageOrientation);
        }

        public void Render()
        {
            _document.Pages.Clear();

            // algorithm:
            // calculate column widths
            // gather rows until no more will fit on this paper
            //   if autofitrows, gather all lines
            //   if repeatrows include these on each page
            //
            //   for each row, gather cells until no more will fit on this paper
            //     if autofitcolumns, gather all cells
            //     if repeatcolumns include these on each page
            //
            //   create page using gathered rows and add to list of pages
            //
            // loop through all pages and create document pages

            // The Graphics instance is used to measure the size of strings
            using (var image = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(image))
            {
                g.PageUnit = GraphicsUnit.Millimeter;
                var columnWidths = CalculateColumnWidths(g, _grid);
                var scale = GetScale(g, columnWidths);

                var pages = new List<IntermediatePage>();

                while (RenderMorePages())
                {
                    // gather as many rows as will fit on this page
                    var pageRows = GatherRows(g, scale);
                    var page = GatherColumns(g, pageRows, columnWidths, scale);

                    pages.Add(page);
                    StepToNextPage(page);
                }

                CreateDocumentPages(pages, scale);
            }
        }

        private double GetScale(Graphics g, IEnumerable<double> columnWidths)
        {
            var scale = 1.0;
            if (_parameters.AutofitColumns)
            {
                var totalWidth = columnWidths.Sum();
                var pageWidthWithoutMargins =
                    DimensionsAndStyle.GetDimensions(_parameters.PageType, _parameters.PageOrientation).Item1 -
                    _parameters.LeftMargin - _parameters.RightMargin;
                scale = Math.Min(scale, pageWidthWithoutMargins / totalWidth);
            }
            if (_parameters.AutofitRows)
            {
                var totalHeight = _grid.Rows.Sum(row => CalculateRowHeight(g, row));
                var pageHeightWithoutMargins =
                    DimensionsAndStyle.GetDimensions(_parameters.PageType, _parameters.PageOrientation).Item2 -
                    _parameters.TopMargin - _parameters.BottomMargin;
                scale = Math.Min(scale, pageHeightWithoutMargins / totalHeight);
            }
            return scale;
        }

        private void StepToNextPage(IntermediatePage page)
        {
            //if (currentColumnOffset == 0)
            //{
            //    currentColumnOffset += page.ColumnCount;
            //}
            //else
            //{
                currentColumnOffset += page.ColumnCount - _parameters.RepeatColumns;
            //}

            if (currentColumnOffset >= (_grid.ColumnCount - _parameters.RepeatColumns))
            {
                currentColumnOffset = 0;
                //if (currentRowOffset == 0)
                //{
                //    currentRowOffset += page.RowCount;
                //}
                //else
                //{
                    currentRowOffset += page.RowCount - _parameters.RepeatRows;
                //}
            }
        }

        public IntermediatePage GatherColumns(Graphics g, List<GridDocumentRow> pageRows, double[] columnWidths, double scale)
        {
            var page = new IntermediatePage();

            // loop through all rows
            double y = _parameters.TopMargin;
            foreach (var row in pageRows)
            {
                double height = CalculateRowHeight(g, row)*scale;
                double x = _parameters.LeftMargin;
                var rowCells = new List<IntermediatePageCell>();
                var summedColumnWidth = _parameters.LeftMargin + _parameters.RightMargin;

                // add repeated columns
                int index;
                for (index = 0; index < _parameters.RepeatColumns; index++)
                {
                    double width = columnWidths[index]*scale;
                    var cell = row.Cells[index];
                    rowCells.Add(new IntermediatePageCell(row, cell, x, y, width, height));
                    summedColumnWidth += width;
                    x += width;
                }

                // add more columns until no more will fit. or all columns if autofitcolumns
                index = currentColumnOffset + _parameters.RepeatColumns;
                while (index < _grid.ColumnCount &&
                       (summedColumnWidth < _documentPageForMetrics.Width || _parameters.AutofitColumns))
                {
                    double width = columnWidths[index]*scale;
                    var cell = row.Cells[index];
                    rowCells.Add(new IntermediatePageCell(row, cell, x, y, width, height));
                    summedColumnWidth += width;
                    x += width;
                    index++;
                }

                y += height;
                page.PageCells.Add(rowCells);
            }

            return page;
        }

        public List<GridDocumentRow> GatherRows(Graphics g, double scale)
        {
            var pageRows = new List<GridDocumentRow>();
            var summedRowHeight = _parameters.TopMargin + _parameters.BottomMargin;

            // add repeated rows
            int index;
            for (index = 0; index < _parameters.RepeatRows; index++)
            {
                var row = _grid.Rows[index];
                pageRows.Add(row);
                summedRowHeight += CalculateRowHeight(g, row)*scale;
            }

            index = _parameters.RepeatRows + currentRowOffset;
            var anyGroupRowsAdded = false;
            // add more groups of rows until no more will fit. or all rows if autofitrows
            // note that if the first group has more rows than can fit the page, we add as many
            // rows as we can.
            while (true)
            {
                var nextGroupRows = GetNextGroupRows(index);
                if (!nextGroupRows.Any()) break;
                var groupHeight = nextGroupRows.Sum(row => CalculateRowHeight(g, row))*scale;
                var pageOverflow = !_parameters.AutofitRows && summedRowHeight + groupHeight >= _documentPageForMetrics.Height;
                if (anyGroupRowsAdded && pageOverflow) break;
                if (pageOverflow)  // This is the first group on the page, but it still won't fit - add as many rows as possible
                {
                    foreach (var row in nextGroupRows)
                    {
                        var rowHeight = CalculateRowHeight(g, row)*scale;
                        if (summedRowHeight + rowHeight >= _documentPageForMetrics.Height) break;
                        summedRowHeight += rowHeight;
                        index++;
                        pageRows.Add(row);
                    }
                    break;
                }
                else
                {
                    pageRows.AddRange(nextGroupRows);
                    index += nextGroupRows.Count;
                    summedRowHeight += groupHeight;
                    anyGroupRowsAdded = true;
                }
            }

            return pageRows;
        }

        private List<GridDocumentRow> GetNextGroupRows(int fromRow)
        {
            var result = new List<GridDocumentRow>();
            if (fromRow == _grid.Rows.Count) return result;
            var firstRow = _grid.Rows[fromRow];
            var group = firstRow.Group;
            result.Add(firstRow);
            if (group == null) return result;
            var currentRow = fromRow + 1;
            while (currentRow < _grid.RowCount)
            {
                var x = _grid.Rows[currentRow];
                if (x.Group != group) break;
                result.Add(x);
                currentRow++;
            }
            return result;
        }

        private void CreateDocumentPages(List<IntermediatePage> pages, double scale)
        {
            foreach (var intermediatePage in pages)
            {
                var page = _document.AddPage(_parameters.PageType, _parameters.PageOrientation, _parameters.TopMargin, _parameters.RightMargin, _parameters.BottomMargin, _parameters.LeftMargin);
                
                // render all cells
                foreach (var row in intermediatePage.PageCells)
                {
                    foreach (var cell in row)
                    {
                        double textSize;
                        Color textColor;
                        Color backgroundColor;
                        DocumentText.TextFlags textFlags;

                        // calculate style from flags
                        StyleCalculator.Set(cell, out textSize, out textColor, out textFlags, out backgroundColor);
                        textSize *= scale;

                        // render background
                        if (backgroundColor != Color.Transparent && backgroundColor != Color.White)
                        {
                            page.AddBox(cell.X, cell.Y, cell.Width, cell.Height, backgroundColor);
                        }

                        // render text
                        if (!string.IsNullOrEmpty(cell.Cell.GetFormattedText(_parameters.CultureInfo)))
                        {
                            page.AddText(cell.X, cell.Y, cell.Width, cell.Height, cell.Cell.GetFormattedText(_parameters.CultureInfo), textSize, textColor, textFlags);
                        }
                    }

                    // Group cells that have the same underline style, so that we can draw one contiguous line rather than
                    // one line for each cell. This helps us avoid issues with small empty spaces between the lines.
                    foreach (var underlinePartition in row.PartitionBy(cell => cell.Cell.UnderlineStyle))
                    {
                        var underlineStyle = underlinePartition.First().Cell.UnderlineStyle;
                        if (underlineStyle == GridDocumentCell.UnderlineStyleType.None) continue;
                        var startX = underlinePartition.First().X;
                        var lastCell = underlinePartition.Last();
                        var endX = lastCell.X + lastCell.Width;
                        var y = lastCell.Y + lastCell.Height;

                        switch (underlineStyle)
                        {
                            case GridDocumentCell.UnderlineStyleType.Subtle:
                                AddUnderlineToPageCell(page, startX, endX, y, 0.15, Color.FromArgb(64, 64, 64), scale);
                                break;
                            case GridDocumentCell.UnderlineStyleType.Normal:
                                AddUnderlineToPageCell(page, startX, endX, y, 0.15, Color.Black, scale);
                                break;
                            case GridDocumentCell.UnderlineStyleType.Thick:
                                AddUnderlineToPageCell(page, startX, endX, y, 0.25, Color.Black, scale);
                                break;
                            case GridDocumentCell.UnderlineStyleType.Double:
                                var thickness = 0.15*scale;
                                var tmpY = y - thickness*4 - 0.5*scale;
                                page.AddLine(startX, tmpY, endX, tmpY, thickness, Color.Black, DocumentLine.LineFlags.Solid);
                                tmpY = y - 0.5*scale;
                                page.AddLine(startX, tmpY, endX, tmpY, thickness, Color.Black, DocumentLine.LineFlags.Solid);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private void AddUnderlineToPageCell(DocumentPage page, double startX, double endX, double y, double thickness, Color color, double scale)
        {
            thickness = thickness*scale;
            y = y - thickness - 0.5*scale;
            page.AddLine(startX, y, endX, y, thickness, color, DocumentLine.LineFlags.Solid);
        }

        public bool RenderMorePages()
        {
            if (currentRowOffset >= (_grid.RowCount - _parameters.RepeatRows))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public double[] CalculateColumnWidths(Graphics g, GridDocument grid)
        {
            var columnWidths = new double[grid.ColumnCount];

            foreach (var row in grid.Rows)
            {
                var columnIndex = 0;
                foreach (var cell in row.Cells)
                {
                    var cellWidth = CalculateCellWidth(g, cell);
                    if (cellWidth > columnWidths[columnIndex])
                    {
                        columnWidths[columnIndex] = cellWidth;
                    }

                    columnIndex++;
                }
            }

            return columnWidths;
        }

        public double CalculateRowHeight(Graphics g, GridDocumentRow row)
        {
            return !row.Cells.Any() ? 3 : row.Cells.Select(cell => CalculateCellHeight(g, cell)).Max();
        }

        private double CalculateCellHeight(Graphics g, GridDocumentCell cell)
        {
            return GetFontHeightInPixels(g, DimensionsAndStyle.GetFontSize(cell.TextStyle));
        }

        public double CalculateCellWidth(Graphics g, GridDocumentCell cell)
        {
            if (cell.Overflow)
            {
                return 0;
            }

            if (string.IsNullOrEmpty(cell.GetFormattedText(_parameters.CultureInfo)))
            {
                return 0;
            }

            return GetStringWidth(g, cell.GetFormattedText(_parameters.CultureInfo), GetFont(cell));
        }

        private Font GetFont(GridDocumentCell cell)
        {
            return new Font(
                "Arial",
                DimensionsAndStyle.GetFontSize(cell.TextStyle),
                DimensionsAndStyle.GetFontStyle(cell.TextStyle));
        }

        private readonly Dictionary<float, float> _fontHeights = new Dictionary<float, float>(); 

        private float GetFontHeightInPixels(Graphics g, float fontSize)
        {
            if (_fontHeights.ContainsKey(fontSize)) return _fontHeights[fontSize];
            var height = MeasureString(g, "x", new Font("Arial", fontSize, FontStyle.Regular)).Height * 1.1f;
            _fontHeights[fontSize] = height;
            return height;
        }

        private float GetStringWidth(Graphics g, string s, Font font)
        {
            return MeasureString(g, s, font).Width * 1.1f;
        }

        private SizeF MeasureString(Graphics g, string s, Font font)
        {
            if (g != null) return g.MeasureString(s, font);
            using (var image = new Bitmap(1, 1))
            using (g = Graphics.FromImage(image))
            {
                return g.MeasureString(s, font);
            }
        }
    }
}