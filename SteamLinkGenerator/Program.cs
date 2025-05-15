using System.Collections.Concurrent;

class Program
{
    #region vars
    private static double valid = 0;
    private static double totalChecked = 0;
    private static double total_val = 0;

    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
    //static readonly char[] symbols = "111NNN".ToCharArray(); //edit character set for custom generation
    static readonly char[] symbols = "abcdefghijklmnopqrstuvwxyz0123456789-_".ToCharArray(); // For all possible variations
    static readonly HttpClient httpClient = new HttpClient();
    static readonly ConcurrentQueue<string> idQueue = new ConcurrentQueue<string>();
    #endregion

    static async Task Main()
    {
        Console.WriteLine("Enter MINIMUM length (minimum allowed is 2):");
        if (!int.TryParse(Console.ReadLine(), out int minLength) || minLength < 2)
        {
            Console.WriteLine("Invalid minimum length.");
            return;
        }

        Console.WriteLine("Enter MAXIMUM length:");
        if (!int.TryParse(Console.ReadLine(), out int maxLength) || maxLength < minLength)
        {
            Console.WriteLine("Invalid maximum length.");
            return;
        }

        Console.WriteLine("Generating IDs...");
        double total = GenerateTheTotalURLNumber(minLength, maxLength);
        Console.WriteLine("TOTAL VARIATIONS: " + total);
        Console.WriteLine("Queueing IDs...");

        for (int len = minLength; len <= maxLength; len++)
        {
            foreach (string id in GenerateIDsOfLength(len))
                idQueue.Enqueue(id);
        }

        Console.WriteLine($"Starting parallel checking ({Environment.ProcessorCount} threads)...");

        await Parallel.ForEachAsync(idQueue, new ParallelOptions
        {
            MaxDegreeOfParallelism = 5
        },
        async (id, token) =>
        {
            await CheckForValid(id, minLength, maxLength);
        });

        Console.WriteLine("Done!");
    }

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

    static async Task ConsoleOutput()
    {
        Console.Clear();
        Console.WriteLine($"CHECKED[{totalChecked}]/TOTAL[{total_val}]/VALID[{valid}]");
    }

}
