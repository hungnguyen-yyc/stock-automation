using StockSignalScanner.Models;

namespace StockSignalScanner.Indicators
{
    internal static class CrossDirectionDetector
    {
        public static CrossDirection GetCrossDirection(List<(DateTime, decimal)> line1, List<(DateTime, decimal)> line2)
        {
            // Check that the lists have at least two elements each
            if (line1.Count < 2 || line2.Count < 2)
            {
                return CrossDirection.NO_CROSS;
            }

            // Initialize variables to track the previous values of line1 and line2
            decimal prevLine1Value = line1[0].Item2;
            decimal prevLine2Value = line2[0].Item2;

            // Initialize a variable to track the cross direction
            CrossDirection crossDirection = CrossDirection.NO_CROSS;

            // Iterate through the rest of the elements in the lists
            for (int i = 1; i < line1.Count; i++)
            {
                // Get the current values of line1 and line2
                decimal currLine1Value = line1[i].Item2;
                decimal currLine2Value = line2[i].Item2;

                // Check if line1 crossed above line2
                if (prevLine1Value < prevLine2Value && (currLine1Value > currLine2Value || currLine1Value == currLine2Value))
                {
                    crossDirection = CrossDirection.CROSS_ABOVE;
                }

                // Check if line1 crossed below line2
                if (prevLine1Value > prevLine2Value && (currLine1Value < currLine2Value || currLine1Value == currLine2Value))
                {
                    crossDirection = CrossDirection.CROSS_BELOW;
                }

                // Update the previous values of line1 and line2 for the next iteration
                prevLine1Value = currLine1Value;
                prevLine2Value = currLine2Value;
            }

            // Return the cross direction
            return crossDirection;
        }

        public static List<(DateTime, CrossDirection)> GetAllCrossDirections(List<(DateTime, decimal)> line1, List<(DateTime, decimal)> line2)
        {
            // Check that the lists have at least two elements each
            if (line1.Count < 2 || line2.Count < 2)
            {
                return new List<(DateTime, CrossDirection)>()
                {
                    (default, CrossDirection.NO_CROSS)
                };
            }

            // Initialize variables to track the previous values of line1 and line2
            decimal prevLine1Value = line1[0].Item2;
            decimal prevLine2Value = line2[0].Item2;

            // Initialize a variable to track the cross direction
            CrossDirection crossDirection = CrossDirection.NO_CROSS;
            var allCrosses = new List<(DateTime, CrossDirection)>();

            // Iterate through the rest of the elements in the lists
            for (int i = 1; i < line1.Count; i++)
            {
                // Get the current values of line1 and line2
                decimal currLine1Value = line1[i].Item2;
                decimal currLine2Value = line2[i].Item2;

                // Check if line1 crossed above line2
                if (prevLine1Value < prevLine2Value && (currLine1Value > currLine2Value || currLine1Value == currLine2Value))
                {
                    crossDirection = CrossDirection.CROSS_ABOVE;
                    allCrosses.Add((line1[i].Item1, crossDirection));
                }

                // Check if line1 crossed below line2
                if (prevLine1Value > prevLine2Value && (currLine1Value < currLine2Value || currLine1Value == currLine2Value))
                {
                    crossDirection = CrossDirection.CROSS_BELOW;
                    allCrosses.Add((line1[i].Item1, crossDirection));
                }

                // Update the previous values of line1 and line2 for the next iteration
                prevLine1Value = currLine1Value;
                prevLine2Value = currLine2Value;
            }

            // Return the cross direction
            return allCrosses;
        }

        public static CrossDirection GetCrossDirection(List<decimal> line1, List<decimal> line2)
        {
            // Check that the lists have at least two elements each
            if (line1.Count < 2 || line2.Count < 2)
            {
                return CrossDirection.NO_CROSS;
            }

            // Initialize variables to track the previous values of line1 and line2
            decimal prevLine1Value = line1[0];
            decimal prevLine2Value = line2[0];

            // Initialize a variable to track the cross direction
            CrossDirection crossDirection = CrossDirection.NO_CROSS;

            // Iterate through the rest of the elements in the lists
            for (int i = 1; i < line1.Count; i++)
            {
                // Get the current values of line1 and line2
                decimal currLine1Value = line1[i];
                decimal currLine2Value = line2[i];

                // Check if line1 crossed above line2
                if (prevLine1Value < prevLine2Value && (currLine1Value > currLine2Value || currLine1Value == currLine2Value))
                {
                    crossDirection = CrossDirection.CROSS_ABOVE;
                }

                // Check if line1 crossed below line2
                if (prevLine1Value > prevLine2Value && (currLine1Value < currLine2Value || currLine1Value == currLine2Value))
                {
                    crossDirection = CrossDirection.CROSS_BELOW;
                }

                // Update the previous values of line1 and line2 for the next iteration
                prevLine1Value = currLine1Value;
                prevLine2Value = currLine2Value;
            }

            // Return the cross direction
            return crossDirection;
        }

        public static Dictionary<DateTime, CrossDirection> GetCrossDirectionWithTime(List<(DateTime, decimal)> line1, List<(DateTime, decimal)> line2)
        {
            // Check that the lists have at least two elements each
            if (line1.Count < 2 || line2.Count < 2)
            {
                return new Dictionary<DateTime, CrossDirection>
                {
                    { default, CrossDirection.NO_CROSS }
                };
            }

            var result = new Dictionary<DateTime, CrossDirection>();
            // Initialize variables to track the previous values of line1 and line2
            decimal prevLine1Value = line1[0].Item2;
            decimal prevLine2Value = line2[0].Item2;

            // Initialize a variable to track the cross direction
            CrossDirection crossDirection = CrossDirection.NO_CROSS;

            // Iterate through the rest of the elements in the lists
            for (int i = 1; i < line1.Count; i++)
            {
                // Get the current values of line1 and line2
                decimal currLine1Value = line1[i].Item2;
                decimal currLine2Value = line2[i].Item2;

                // Check if line1 crossed above line2
                if (prevLine1Value < prevLine2Value && currLine1Value > currLine2Value)
                {
                    crossDirection = CrossDirection.CROSS_ABOVE;
                }

                // Check if line1 crossed below line2
                if (prevLine1Value > prevLine2Value && currLine1Value < currLine2Value)
                {
                    crossDirection = CrossDirection.CROSS_BELOW;
                }

                result.Add(line1[i].Item1, crossDirection);
                // Update the previous values of line1 and line2 for the next iteration
                prevLine1Value = currLine1Value;
                prevLine2Value = currLine2Value;
            }

            // Return the cross direction
            return result;
        }

        public static DateTime GetDateOfCross(List<(DateTime, decimal)> line1, List<(DateTime, decimal)> line2)
        {
            // Check that the lists have at least two elements each
            if (line1.Count < 2 || line2.Count < 2)
            {
                return default;
            }

            var result = new Dictionary<DateTime, CrossDirection>();
            // Initialize variables to track the previous values of line1 and line2
            decimal prevLine1Value = line1[0].Item2;
            decimal prevLine2Value = line2[0].Item2;

            // Initialize a variable to track the cross direction
            CrossDirection crossDirection = CrossDirection.NO_CROSS;

            // Iterate through the rest of the elements in the lists
            for (int i = 1; i < line1.Count; i++)
            {
                // Get the current values of line1 and line2
                decimal currLine1Value = line1[i].Item2;
                decimal currLine2Value = line2[i].Item2;

                // Check if line1 crossed above line2
                if (prevLine1Value < prevLine2Value && currLine1Value > currLine2Value)
                {
                    return line1[i].Item1;
                }

                // Check if line1 crossed below line2
                if (prevLine1Value > prevLine2Value && currLine1Value < currLine2Value)
                {
                    return line1[i].Item1;
                }

                result.Add(line1[i].Item1, crossDirection);
                // Update the previous values of line1 and line2 for the next iteration
                prevLine1Value = currLine1Value;
                prevLine2Value = currLine2Value;
            }

            // Return the cross direction
            return default;
        }
    }
}