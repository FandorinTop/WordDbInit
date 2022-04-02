using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WordDbInit
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var countFilePath = @"C:\Users\Shewc\source\repos\WordDbInit\WordDbInit\Count.txt";
            
            if (!File.Exists(countFilePath))
            {
                using (var tt = File.Create(countFilePath))
            {
                File.CreateText(countFilePath);
            }
            }
            
            long position = 0;

            using (StreamReader reader = new StreamReader(countFilePath))
            {
                position = Convert.ToInt64(reader.ReadLine() ?? "0");
            }

            var fileName = @"C:\Users\Shewc\source\repos\WordDbInit\WordDbInit\words.txt";
            //string connectionString = @"Server=(localdb)\\mssqllocaldb;Database=FandorinTopTelegramBot;Trusted_Connection=True;";
            
            var builder = new ConfigurationBuilder();
            // установка пути к текущему каталогу
            builder.SetBasePath(Directory.GetCurrentDirectory());
            // получаем конфигурацию из файла appsettings.json
            builder.AddJsonFile(@"C:\Users\Shewc\source\repos\WordDbInit\WordDbInit\appsettings.json");
            // создаем конфигурацию
            var config = builder.Build();
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            var options = optionsBuilder
                .UseSqlServer(config.GetConnectionString("DefaultConnection"))
                .UseLazyLoadingProxies()
                .Options;


            var service = new InitDb(options);

            using (StreamReader reader = new StreamReader(fileName))
            {
                long counter = (int) position;
                int size = 10;

                var lines = await File.ReadAllLinesAsync(fileName);

                for (int i = (int)position; i < lines.Length; i+= size)
                {
                    bool IsSkip = false;
                    //lines.Skip(i).Take(i + size).ToList().AsParallel().ForAll(line =>
                    //{
                    //    try
                    //    {
                    //        service.MiddlewareInwoke(line).GetAwaiter().GetResult();
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Console.WriteLine($"Exception for word: '{line}' ExMessage: '{ex.Message.ToString()}'");
                    //    }
                    //});

                    //service.MiddlewareInwoke(lines[i]).GetAwaiter().GetResult();

                    //var taskList = new List<Task>();

                    //foreach (var line in lines.Skip(i).Take(size).ToList())
                    //{
                    //    var task = new Task(() => service.MiddlewareInwoke(line).GetAwaiter().GetResult());
                    //    taskList.Add(task);
                    //}

                    //foreach (var item in taskList)
                    //{
                    //    item.Start();
                    //}

                    //await Task.WhenAll(taskList.ToArray());

                    var tasks = Parallel.ForEach(lines.Skip(i).Take(size).ToList(), (line) =>
                    {
                        Console.WriteLine($"Number: '{counter++}' value = {line}");
                        try
                        {
                            service.MiddlewareInwoke(line).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Exception for word: '{line}' ExMessage: '{ex.Message.ToString()}'");
                        }
                    });

                    Console.WriteLine($"End for: {i}");
                    
                    using (var tt = File.CreateText(countFilePath))
                    {
                        tt.WriteLine(i);
                    }

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Proggress: {((double)i / lines.Length):P02}");
                    Console.WriteLine($"Proggress: {((double)i / lines.Length):P02}");
                    Console.WriteLine($"Proggress: {((double)i / lines.Length):P02}");
                    Console.ResetColor();

                    IsSkip = !IsSkip;

                    if (IsSkip)
                    {
                        Thread.Sleep(1000 * size / 2);
                    }
                    else
                    {
                        Thread.Sleep(1000 * size / 8);
                    }
                }
            }
        }
    }

    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public virtual DbSet<WordEntity> Words { get; set; }
        public virtual DbSet<WordType> WordTypes { get; set; }
        public virtual DbSet<WordToWordConnection> WordConnections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WordToWordConnection>()
                .HasOne(e => e.CWord)
                .WithMany(e => e.PWords)
                .HasForeignKey(e => e.CWordId);

            modelBuilder.Entity<WordToWordConnection>()
               .HasOne(e => e.PWord)
               .WithMany(e => e.CWords)
               .HasForeignKey(e => e.PWordId);
        }
    }

    public class WordToWordConnection : BaseEntity
    {
        public long PWordId { get; set; }

        [ForeignKey(nameof(PWordId))]
        public virtual WordEntity PWord { get; set; }

        public long CWordId { get; set; }

        [ForeignKey(nameof(CWordId))]
        public virtual WordEntity CWord { get; set; }
    }

    public class BaseEntity
    {
        public BaseEntity()
        {
            CreationTime = DateTime.UtcNow;
        }

        [Key]
        public long Id { get; set; }

        public DateTime CreationTime { get; set; }
    }

    public class WordType : BaseEntity
    {
        [Required]
        [MaxLength(60)]
        public string Type { get; set; }

        public virtual List<WordTypeConncetion> Words { get; set; } = new List<WordTypeConncetion>();
    }

    public class WordTypeConncetion : BaseEntity
    {
        public long WordTypeId { get; set; }

        [ForeignKey(nameof(WordTypeId))]
        public virtual WordType WordType { get; set; }

        public long WordEntityId { get; set; }

        [ForeignKey(nameof(WordEntityId))]
        public virtual WordEntity Word { get; set; }
    }

    [Index(nameof(NormalizedWord), IsUnique = true)]
    public class WordEntity : BaseEntity
    {
        private string word;

        [MaxLength(80)]
        public string Word
        {
            get
            {
                return word;
            }
            set
            {
                word = value?.Trim();
                NormalizedWord = word?.Trim()?.ToLowerInvariant();
            }
        }

        [MaxLength(80)]
        public string NormalizedWord { get; private set; }

        public Language Language { get; set; } = Language.English;

        public virtual List<WordTypeConncetion> WordTypes { get; set; } = new List<WordTypeConncetion>();

        public virtual List<WordToWordConnection> CWords { get; set; } = new List<WordToWordConnection>();

        public virtual List<WordToWordConnection> PWords { get; set; } = new List<WordToWordConnection>();
    }
    public enum Language
    {
        English = 1,
        Russia = 2
    }

    public class InitDb
    {
        DbContextOptions<ApplicationDbContext> _options;

        public InitDb(DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public async Task MiddlewareInwoke(string inputWord)
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

            using (HttpClient httpClient = new HttpClient())
            {
                var normalizedWord = inputWord.ToLowerInvariant().Replace("-", "").Trim();

                using (ApplicationDbContext _context = new ApplicationDbContext(_options))
                {
                    var temp2 = await _context.Words
                                   .Include(cword => cword.CWords)
                                   .Include(cword => cword.PWords)
                                   .Include(type => type.WordTypes)
                                   .Where(item => item.Language == Language.English)
                                   .FirstOrDefaultAsync(item => item.Id == 21);

                    var temp = await _context.Words
                                   .Include(cword => cword.CWords)
                                   .Include(type => type.WordTypes)
                                   .FirstOrDefaultAsync(item => item.NormalizedWord == normalizedWord);

                    if(temp != null)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"Already Exist: {inputWord}");
                        Console.ResetColor();
                        return;
                    }
                }

                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");

                while (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    response = await httpClient.GetAsync("https://context.reverso.net/translation/english-russian/" + normalizedWord);

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        int threadBaseSleep = 2;
                        response.Headers.TryGetValues("Retry-After", out var count);
                        var value = count.FirstOrDefault() ?? "0";

                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"Thread.Sleep for {Convert.ToInt32(value) + threadBaseSleep}");
                        Console.ResetColor();
                        Thread.Sleep(TimeSpan.FromSeconds(Convert.ToInt32(value) + threadBaseSleep));
                    }
                }

                var result = await response.Content.ReadAsStringAsync();

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(result);
                var translation = htmlDocument.GetElementbyId("translations-content");

                if (translation != null)
                {
                    var ChildNodes = translation.ChildNodes;
                    var selected = ChildNodes.Where(item => item.InnerHtml.Contains("pos-mark")).ToList();

                    using (ApplicationDbContext _context = new ApplicationDbContext(_options))
                    {
                        WordEntity word = null;

                        try
                        {
                            word = await _context.Words
                                .Include(cword => cword.CWords)
                                .Include(type => type.WordTypes)
                                .FirstOrDefaultAsync(item => item.NormalizedWord == normalizedWord);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        if (word == null)
                        {
                            word = new WordEntity();
                            word.Language = Language.English;
                            word.Word = normalizedWord;
                            await _context.Words.AddAsync(word);
                            await _context.SaveChangesAsync();
                        }

                        foreach (var item in selected)
                        {
                            var childNodes3 = item.ChildNodes.FirstOrDefault(cN => cN.InnerHtml.Contains("title="));

                            if (childNodes3 == null)
                            {
                                continue;
                            }

                            var val = childNodes3.ChildNodes.FirstOrDefault(ss => ss.ChildAttributes("class").Any() && ss.ChildAttributes("title").Any());
                            var wordTypeValue = val.GetAttributeValue("class", "");
                            WordType wordType = await _context.WordTypes.FirstOrDefaultAsync(item => item.Type == wordTypeValue);

                            if (wordType == null)
                            {
                                wordType = new WordType()
                                {
                                    Type = wordTypeValue
                                };

                                await _context.WordTypes.AddAsync(wordType);
                                await _context.SaveChangesAsync();
                            }

                            var wordHasWordType = word.WordTypes.FirstOrDefault(wt => wt.WordTypeId == wordType.Id);

                            if (wordHasWordType == null)
                            {
                                word.WordTypes.Add(new WordTypeConncetion()
                                {

                                    WordType = wordType,
                                    Word = word
                                });

                                await _context.SaveChangesAsync();
                            }

                            var normInnerWord = item.InnerText.Split("\r\n     ")
                                    .Select(item => item.Trim())
                                    .Where(item => !string.IsNullOrWhiteSpace(item))
                                    .FirstOrDefault();

                            WordEntity innerWord = await _context.Words
                                .Include(word => word.WordTypes)
                                .FirstOrDefaultAsync(word => word.NormalizedWord.Equals(normInnerWord));

                            if (innerWord == null)
                            {
                                innerWord = new WordEntity()
                                {
                                    Language = Language.Russia,
                                    Word = normInnerWord,
                                    WordTypes = new List<WordTypeConncetion>() { new WordTypeConncetion(){

                                            WordType = wordType,
                                            Word = innerWord
                                        },
                                        }
                                };

                                await _context.Words.AddAsync(innerWord);
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                var innerHasWordType = innerWord.WordTypes.FirstOrDefault(wt => wt.WordTypeId == wordType.Id);

                                if (innerHasWordType == null)
                                {
                                    innerWord.WordTypes.Add(new WordTypeConncetion()
                                    {

                                        WordType = wordType,
                                        Word = word
                                    });
                                }

                                await _context.SaveChangesAsync();
                            }

                            var connection = new WordToWordConnection()
                            {
                                CWord = word,
                                PWord = innerWord
                            };

                            _context.WordConnections.Add(connection);

                            //word.CWords.Add(new WordToWordConnection()
                            //{
                            //    CWord = word,
                            //    PWord = innerWord
                            //});

                            var count = await _context.SaveChangesAsync();
                        }

                        var arr = translation.InnerText.Split("\r\n     ")
                                .Select(item => item.Trim())
                                .Where(item => !string.IsNullOrWhiteSpace(item))
                                .ToList();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Word: '{inputWord}', Added");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"Translation Error for: '{inputWord}', Responce: {response}");
                    Console.ResetColor();
                }
            }
        }
    }

}
