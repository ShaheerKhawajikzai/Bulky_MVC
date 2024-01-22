namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int? test = 1;

            if (test.GetValueOrDefault() == 1)
            {
                Console.WriteLine("Inside If ");
            }
        }
    }
}