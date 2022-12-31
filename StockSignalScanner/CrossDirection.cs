using StockSignalScanner.Models;

namespace StockSignalScanner
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
                if (prevLine1Value < prevLine2Value && currLine1Value > currLine2Value)
                {
                    crossDirection = CrossDirection.CROSS_ABOVE;
                }

                // Check if line1 crossed below line2
                if (prevLine1Value > prevLine2Value && currLine1Value < currLine2Value)
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
    }
}