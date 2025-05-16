using System.Collections.Concurrent;

class Program
{
    #region Settings
    private static double valid = 0;
    private static double totalChecked = 0;
    private static double total_val = 0;

    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
    static char[] symbols = "abcdefghijklmnopqrstuvwxyz0123456789-_".ToCharArray(); //For all possible variations
    static readonly HttpClient httpClient = new HttpClient();
    static readonly ConcurrentQueue<string> idQueue = new ConcurrentQueue<string>();
    #endregion

    #region Main | Setting up
    static async Task Main()
    {
        Console.WriteLine("1. ALL possible characters");
        Console.WriteLine("2. CUSTOM character set");
        if (!int.TryParse(Console.ReadLine(), out int answ) || answ != 1 && answ != 2)
        {
            Console.WriteLine();
            Console.WriteLine("Invalid option");
            return;
        }
        else
        {
            if (answ == 2)
            {
                Console.WriteLine();
                Console.WriteLine("Enter the CUSTOM character set");
                string set = Console.ReadLine();
                if (!string.IsNullOrEmpty(set))
                {
                    symbols = set.ToCharArray();
                    Console.WriteLine("Your character set is: " + new string(symbols));
                }
                else
                {
                    Console.WriteLine("Invalid input. Using default character set");
                }
            }
            else if (answ == 1)
            {
                Console.WriteLine("Your character set is: " + new string(symbols));
            }
        }

        Console.WriteLine();
        Console.WriteLine("Enter MINIMUM length (minimum allowed is 2):");
        if (!int.TryParse(Console.ReadLine(), out int minLength) || minLength < 2)
        {
            Console.WriteLine();
            Console.WriteLine("Invalid minimum length");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Enter MAXIMUM length:");
        if (!int.TryParse(Console.ReadLine(), out int maxLength) || maxLength < minLength)
        {
            Console.WriteLine();
            Console.WriteLine("Invalid maximum length");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Choose the number of threads:");
        if (!int.TryParse(Console.ReadLine(), out int threads))
        {
            Console.WriteLine();
            Console.WriteLine("Invalid input");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Generating IDs...");
        double total = GenerateTheTotalURLNumber(minLength, maxLength);
        Console.WriteLine("TOTAL VARIATIONS: " + total);
        Console.WriteLine("Queueing IDs...");

        for (int len = minLength; len <= maxLength; len++)
        {
            foreach (string id in GenerateIDsOfLength(len))
                idQueue.Enqueue(id);
        }

        Console.WriteLine($"Starting with [{threads}] threads...");

        await Parallel.ForEachAsync(idQueue, new ParallelOptions
        {
            MaxDegreeOfParallelism = threads
        },
        async (id, token) =>
        {
            await CheckForValid(id, minLength, maxLength);
        });

        Console.WriteLine("DONE");
    }
    #endregion

    #region Generation/Convertion
    static double GenerateTheTotalURLNumber(int min, int max)
    {
        double total = 0;
        for (int x = min; x <= max; x++)
            total += Math.Pow(symbols.Length, x);
        total_val = total;
        return total;
    }

    static IEnumerable<string> GenerateIDsOfLength(int length)
    {
        long total = (long)Math.Pow(symbols.Length, length);
        for (long i = 0; i < total; i++)
            yield return IndexToID(i, length);
    }

    static string IndexToID(long index, int length)
    {
        char[] id = new char[length];
        for (int i = length - 1; i >= 0; i--)
        {
            id[i] = symbols[index % symbols.Length];
            index /= symbols.Length;
        }
        return new string(id);
    }
    #endregion

    #region CheckForValid
    static async Task CheckForValid(string id, int minLenght, int maxLength)
    {
        string url = $"https://steamcommunity.com/id/{id}";
        totalChecked += 1;
        try
        {
            string html = await httpClient.GetStringAsync(url);

            if (html.Contains("<title>Steam Community :: Error</title>"))
            {
                //Console.WriteLine($"FREE: {id}");
                valid += 1;
                await WriteToFile($"{minLenght}-{maxLength} len.txt", id + Environment.NewLine);
            }
            else
            {
                //Console.WriteLine($"TAKEN: {id}");
            }

            await ConsoleOutput();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR checking {id}: {ex.Message}");
        }
    }
    #endregion

    #region WriteToFile
    static async Task WriteToFile(string filePath, string content)
    {
        await semaphore.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(filePath, content);
        }
        finally
        {
            semaphore.Release();
        }
    }
    #endregion

    #region Output

    static async Task ConsoleOutput()
    {
        Console.Clear();
        Console.WriteLine($"CHECKED[{totalChecked}]/TOTAL[{total_val}]/VALID[{valid}]");
    }
    #endregion

}
