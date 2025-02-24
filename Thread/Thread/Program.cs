using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

class Program
{
    static void Main()
    {
        //Выводим спецификацию ПК
        Console.WriteLine("=== Системная информация ===");

        // Основная информация
        Console.WriteLine($"ОС: {GetOSInfo()}");
        Console.WriteLine($"Имя компьютера: {Environment.MachineName}");
        Console.WriteLine($"Логические процессоры: {Environment.ProcessorCount}");
        Console.WriteLine($"Память: {GetPhysicalMemory()} GB");
        Console.WriteLine($"Архитектура: {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}");
        // Информация о процессоре
        Console.WriteLine($"Процессор: {GetCpuInfo()}");
        // Дополнительная информация
        Console.WriteLine($"Версия .NET: {Environment.Version}\n");

        int[] sizes = { 100_000, 1_000_000, 10_000_000 };

        foreach (int size in sizes)
        {
            int[] array = GenerateArray(size);

            Console.WriteLine($"Размер массива: {size:N0}");

            // Обычное вычисление
            MeasureTime("Последовательное", () => SimpleSum(array));

            // Параллельное с потоками
            MeasureTime("Параллельное (Thread)", () => ParallelSumWithThreads(array));

            // Параллельное через LINQ
            MeasureTime("Параллельное (LINQ)", () => ParallelSumWithLinq(array));

            Console.WriteLine();
        }
    }

    static int[] GenerateArray(int size)
    {
        Random rnd = new Random();

        return Enumerable.Range(0, size)
                         .Select(_ => rnd.Next(1, 100))
                         .ToArray();
    }

    static long SimpleSum(int[] array)
    {
        long sum = 0;

        foreach (int num in array) sum += num;
        return sum;
    }

    static long ParallelSumWithThreads(int[] array)
    {
        int threadsCount = Environment.ProcessorCount;
        long[] partialSums = new long[threadsCount];
        Thread[] threads = new Thread[threadsCount];

        int chunkSize = array.Length / threadsCount;

        for (int i = 0; i < threadsCount; i++)
        {
            int threadIndex = i;
            int start = i * chunkSize;
            int end = (threadIndex == threadsCount - 1) ? array.Length : start + chunkSize;

            threads[threadIndex] = new Thread(() =>
            {
                long localSum = 0;

                for (int j = start; j < end; j++) localSum += array[j];
                partialSums[threadIndex] = localSum;
            });

            threads[threadIndex].Start();
                
            //Console.WriteLine($"Запущен поток {i}: chunkSize = {chunkSize}, start = {start}, end = {end}");
        }

        foreach (var thread in threads) thread.Join();
        return partialSums.Sum();
    }

    static long ParallelSumWithLinq(int[] array) => array.AsParallel().Sum(x => (long)x);

    static void MeasureTime(string methodName, Func<long> action)
    {
        Stopwatch sw = Stopwatch.StartNew();
        long sum = action();
        sw.Stop();

        Console.WriteLine($"{methodName}:");
        Console.WriteLine($"  Сумма: {sum:N0} Время: {sw.Elapsed.TotalMilliseconds:F2} мс");
    }

    static string GetOSInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
            foreach (ManagementObject os in searcher.Get())
            {
                return $"{os["Caption"]} (Версия: {os["Version"]})";
            }
        }
        return RuntimeInformation.OSDescription;
    }

    static string GetCpuInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["Name"].ToString().Trim();
            }
        }
        catch { }
        return "Не удалось определить процессор";
    }

    static double GetPhysicalMemory()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (ulong.TryParse(obj["TotalPhysicalMemory"]?.ToString(), out ulong memory))
                {
                    return Math.Round(memory / (1024.0 * 1024 * 1024), 1);
                }
            }
        }
        catch { }
        return 0;
    }
}