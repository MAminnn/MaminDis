namespace MaminDis.Utils
{
    public class UniqueRandomNumberGenerator : Random
    {
        private int minValue;
        private int maxValue;
        private static int[] generatedNumbers;
        public UniqueRandomNumberGenerator(int minValue, int maxValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
            generatedNumbers = new int[maxValue - minValue];
        }

        public override int Next()
        {
            int randomNumber = base.Next(minValue, maxValue);
            while (generatedNumbers.Any(number => number == randomNumber))
            {
                randomNumber = base.Next(minValue, maxValue);
            }
            generatedNumbers.Append(randomNumber);
            return randomNumber;
        }
    }
}
