using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Task1;

public class MainClass
{
    private static int _totalCount;
    private static int _definitelyCount;
    private static int _ambiguousCount;
    private static int _possibleLemmasCount;
    
    private const string MajorData = "C:\\Users\\mikha\\RiderProjects\\Linguistics\\minor.txt";
    private const string OpenDict = "C:\\Users\\mikha\\RiderProjects\\Linguistics\\test.xml";
    private const string Output = "C:\\Users\\mikha\\RiderProjects\\Linguistics\\output.txt";
    private const string Error = "C:\\Users\\mikha\\RiderProjects\\Linguistics\\errors.txt";

    private static BlockingCollection<string> _lines = new();
    private static ConcurrentDictionary<string, int> _frequency = new();

    private static readonly TextReader Reader = TextReader.Synchronized(
        new StreamReader(MajorData, Encoding.UTF8));

    public static void Main(string[] args)
    {
        CorporaParser corp = new CorporaParser(OpenDict);
        Console.WriteLine("Mapping size: " + corp.LemmatizedForms.Count);
        
        ConcurrentDictionary<string, int> errors = new();
        List<string>? tokens;

        Thread newThread = new Thread(Run);
        newThread.Start();
        Thread.Sleep(50);
        while (!_lines.IsCompleted)
        {
            var current = _lines.Take();
            tokens = Tokenize(current);
            foreach (var unused in tokens)
            {
                Parallel.ForEach(tokens, i =>
                {
                    if (corp.LemmatizedForms.TryGetValue(i, out var pl))
                    {
                        Count(pl);
                    }
                    else
                    {
                        if (errors.TryGetValue(i, out _))
                        {
                            errors[i]++;
                        }
                        else
                        {
                            errors.TryAdd(i, 1);
                        }
                    }
                });
            }
        }

        Reader.Close();
        Reader.Dispose();
        _lines.Dispose();

        LogReport();

        var sortedFreq =
            from entry
                in _frequency
            orderby entry.Value descending
            select entry;

        var sortedErr =
            from entry
                in errors
            orderby entry.Value descending
            select entry;

        LogToFile(sortedFreq,Output);
        LogToFile(sortedErr,Error);

        Console.WriteLine("Done");
    }

    private static List<string> Tokenize(string text)
    {
        text = Regex.Replace(text, "[^а-яА-Я]", " ");
        return text
            .ToLower()
            .Split(' ')
            .Where((x) => (x.Length) >= 2)
            .ToList();
    }

    private static void Run()
    {
        string? nextLine;
        while ((nextLine = Reader.ReadLine()) != null)
        {
            _lines.Add(nextLine);
        }

        _lines.CompleteAdding();
    }

    private static void Count(HashSet<WordForm> lemmas)
    {
        Interlocked.Increment(ref _totalCount);
        if (lemmas.Count == 1)
        {
            Interlocked.Increment(ref _definitelyCount);
        }
        else if (lemmas.Count > 1)
        {
            Interlocked.Increment(ref _ambiguousCount);
        }

        Interlocked.Add(ref _possibleLemmasCount, lemmas.Count);

        Parallel.ForEach(lemmas, l =>
        {
            if (_frequency.TryGetValue(l.InitialWord, out _))
            {
                _frequency[l.InitialWord]++;
            }
            else
            {
                _frequency.TryAdd(l.InitialWord, 1);
            }
        });
    }

    private static void LogReport()
    {
        Console.WriteLine("Tokens count: " + _totalCount);
        Console.WriteLine("Found unique lemmas :" + _frequency.Count);
        Console.WriteLine("Found lemmas for " + (_definitelyCount + _ambiguousCount) + " tokens over total "
                          + _totalCount + " tokens (" + (1.0 * _definitelyCount + _ambiguousCount) / _totalCount + ")");
        Console.WriteLine("Definite tokens: " + _definitelyCount);
        Console.WriteLine("Ambiguous tokens: " + _ambiguousCount);
        Console.WriteLine("Possible lemmas count: " + _possibleLemmasCount);
        Console.WriteLine("Not lemmatized: " + (_totalCount - _definitelyCount - _ambiguousCount));
    }

    private static void LogToFile(IOrderedEnumerable<KeyValuePair<string,int>> collection,
        string path)
    {
        TextWriter writer = new StreamWriter(path);
        foreach (KeyValuePair<string, int> kvp in collection)
        {
            writer.WriteLine(kvp.Value + " " + kvp.Key);
        }
        writer.Flush();
        writer.Close();
        writer.Dispose();
    }
}