using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Store;
using Newtonsoft.Json;
using PorterStemmerAlgorithm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaselineSystem
{
    public class Result
    {
        public string rank { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public string passage_text { get; set; }
    }
    public class LuceneSearchEngine
    {
        Lucene.Net.Store.Directory directory;
        private string collectionPath;
        public string CollectionPath { get => collectionPath; set => collectionPath = value; }
        string indexPath;
        public string IndexPath
        {
            get { return indexPath; }
            set
            {
                if (System.IO.Directory.Exists(value))
                {
                    indexPath = value;
                    directory = FSDirectory.Open(indexPath);// Open the directory of index
                    //directory = new RAMDirectory();
                }
            }
        }
        const Lucene.Net.Util.Version VERSION = Lucene.Net.Util.Version.LUCENE_30;
        const string URL_FN = "URL";
        const string PASSAGE_FN = "Passage";
        const string TEXT_FN = "Text";
        const string TITLE_FN = "Title";
        const string PASSAGE_ID_FN = "Passage_id";
        Analyzer analyzer;
        IndexSearcher searcher;
        private long buildIndexTime;
        public long BuildIndexTime { get => buildIndexTime; }
        public event EventHandler<LogEventArgs> LogEvent;
        static string[] stopWords = { "a", "about", "above", "across", "after", "afterwards", "again", "against", "all", "almost", "alone", "along", "already", "also", "although",
                                      "always", "am", "among", "amongst", "amount", "an", "and", "another", "any", "anyhow", "anyone", "anything", "anyway", "anywhere", "are",
                                      "around", "as", "at","back","be", "been", "before", "beforehand", "behind", "being", "below", "beside", "between", "beyond", "both", "bottom", "but", "by",
                                      "can", "could", "each", "else", "empty", "enough", "etc", "even", "ever", "every", "few", "for", "from", "had", "has", "have", "he", "hence", "her", "here",
                                      "hereafter", "hereby", "herein", "hereupon", "hers", "herself", "him", "himself", "his", "how", "however", "if", "in", "into", "is", "it", "may", "me",
                                      "meanwhile", "might", "mine", "much", "must", "my", "myself", "neither", "never", "nevertheless", "next", "no", "nobody", "none", "nor", "not", "nothing", "now", "nowhere",
                                      "of", "on", "or", "other", "our", "over", "out", "per", "please", "put", "rather", "same", "should", "so", "still", "such", "than", "that", "the", "their", "then", "there",
                                      "these", "they", "this", "those", "through", "thus","to", "un", "up", "very", "via", "was", "what", "when", "where", "which", "while", "who", "whom", "whose", "why", "will",
                                      "with", "would", "yet", "you", "your"};

        public List<string> SaveResults { get; }
        public LuceneSearchEngine()
        {
            analyzer = new StandardAnalyzer(VERSION);
            SaveResults = new List<string>();
        }

        public void BuildIndex()
        {
            if (collectionPath is null || !System.IO.Directory.Exists(collectionPath))
            {
                throw new Exception("Please set collection path properly!");
            }
            if (indexPath is null || !System.IO.Directory.Exists(indexPath))
            {
                throw new Exception("Please set index path properly!");
            }

            List<string> collectionFiles = new List<string>();// The list of files
            collectionFiles.Clear();// cleaning the list 
            FindFile(collectionPath, ref collectionFiles);// save the files in collection path in the list
            Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();// start counting
            IndexWriter.MaxFieldLength mfl = new IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH);
            IndexWriter writer = new IndexWriter(directory, analyzer, true, mfl);
            writer.SetSimilarity(new MySimilarity());
            var watis = new List<EventWaitHandle>();//For thread synchronization, wait for all child threads to finish
            foreach (string filepath in collectionFiles)
            {
                LogEvent(this, new LogEventArgs("File Reading: " + Path.GetFileName(filepath) + " ..."));
                string json = string.Empty;
                using (FileStream fs = new FileStream(filepath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        json = sr.ReadToEnd().ToString();// read the text in .json to the string
                        //Convert the json string into a list of Element objects
                        LogEvent(this, new LogEventArgs("Convert json to object..."));
                        List<Element> elements = JsonConvert.DeserializeObject<List<Element>>(json);
                        LogEvent(this, new LogEventArgs("Index Building..."));
                        // use multi thread to build the index
                        int threadNumber = 2;//the numer of the threads
                        // split the list
                        List<List<Element>> listGroup = new List<List<Element>>();
                        int j = elements.Count / threadNumber;
                        for (int i = 0; i < threadNumber; i += 1)
                        {
                            List<Element> cList = new List<Element>();
                            if (i == threadNumber-1)
                                cList = elements.Take(elements.Count).Skip(j * i).ToList();
                            else
                                cList = elements.Take(j * (i + 1)).Skip(j * i).ToList();
                            listGroup.Add(cList);
                        }
                        // Thread
                        ParameterizedThreadStart threadStart = param =>
                        {
                            Tuple<List<Element>, EventWaitHandle> t = (Tuple<List<Element>, EventWaitHandle>)param;
                            foreach (var itme in (List<Element>)t.Item1)
                            {
                                foreach (var passage in itme.passages)// preprocess the passage in the collection file
                                {
                                    string passage_text = PreProcess(passage.passage_text, true);
                                    IndexText(writer, passage.url, passage.title, passage_text, passage.passage_text, passage.passage_id.ToString());
                                }
                            }
                            t.Item2.Set();// set a singal
                        };
                        // start the trread
                        foreach( var list in listGroup)
                        {
                            var handler = new ManualResetEvent(false);
                            watis.Add(handler);
                            Thread thread = new Thread(threadStart);
                            thread.Start(new Tuple<List<Element>, EventWaitHandle>(list, handler));
                        }
                        
                    }
                }
            }
            WaitHandle.WaitAll(watis.ToArray());//wait for thread end
            watch.Stop();// End of timeing 
            buildIndexTime = watch.ElapsedMilliseconds;// save the time
            LogEvent(this, new LogEventArgs("Index Complete. time cost:" + buildIndexTime / 1000.0f + "second"));// display time in LogEvent
            CleanUpIndexer(writer);
        }
        /// <summary>       
        /// </summary>
        /// <param name="path"></param>
        /// <param name="files"></param>
        void FindFile(string path, ref List<string> files)
        {
            foreach (string filename in System.IO.Directory.GetFiles(path, "*.json"))
                files.Add(filename);
            foreach (string dir in System.IO.Directory.GetDirectories(path))
                FindFile(dir, ref files);
        }
        /// <summary>
        /// Indexes a given string into the index
        /// </summary>
        /// <param name="query"></param>
        /// <param name="answer"></param>
        /// <param name="passage"></param>        
        void IndexText(IndexWriter writer, string url, string title, string passage, string text, string passage_id)
        {
            Field urlFiled = new Field(URL_FN, url, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS, Field.TermVector.NO);
            Field titleField = new Field(TITLE_FN, title, Field.Store.YES, Field.Index.ANALYZED_NO_NORMS, Field.TermVector.YES);
            Field passageField = new Field(PASSAGE_FN, passage, Field.Store.NO, Field.Index.ANALYZED_NO_NORMS, Field.TermVector.YES);
            Field textField = new Field(TEXT_FN, text, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS, Field.TermVector.NO);
            Field passageidField = new Field(PASSAGE_ID_FN, passage_id, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS, Field.TermVector.NO);
            Document doc = new Document();
            doc.Add(urlFiled);
            doc.Add(titleField);
            doc.Add(passageField);
            doc.Add(textField);
            doc.Add(passageidField);
            writer.AddDocument(doc);
        }
        /// <summary>
        /// Flushes the buffer and closes the index
        /// </summary>
        void CleanUpIndexer(IndexWriter writer)
        {
            writer.Optimize();
            writer.Flush(true, true, true);
            writer.Dispose();
        }
        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="querytext"></param>
        /// <param name="preprocess"></param>
        /// <returns></returns>
        public List<Result> Search(string querytext, bool preprocess)
        {
            List<Result> results = new List<Result>();
            searcher = new IndexSearcher(directory);
            QueryParser parser;
            string[] fields = { TITLE_FN, PASSAGE_FN };
            var boosts = new Dictionary<string, float> { { TITLE_FN, 3 }, { PASSAGE_FN, 1 } };
            //parser = new QueryParser(VERSION, TITLE_FN, analyzer);
            //parser = new MultiFieldQueryParser(VERSION, fields, analyzer);
            parser = new MultiFieldQueryParser(VERSION, fields, analyzer, boosts);

            if (preprocess)
                querytext = PreProcess(querytext, false);
            Query query = parser.Parse(querytext);

            LogEvent(this, new LogEventArgs("Start Search..."));
            Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            TopDocs topDocs = searcher.Search(query, 1000);
            watch.Stop();
            buildIndexTime = watch.ElapsedMilliseconds;
            LogEvent(this, new LogEventArgs("Search Completed! time cost:" + buildIndexTime / 1000.0f + "second"));
            int i = 0;
            int rank = 0;
            foreach (ScoreDoc scoreDoc in topDocs.ScoreDocs)
            {
                rank++;
                Document doc = searcher.Doc(scoreDoc.Doc);
                string title = doc.Get(TITLE_FN).ToString();
                string url = doc.Get(URL_FN).ToString();
                string passage_text = doc.Get(TEXT_FN).ToString();
                string passage_id = doc.Get(PASSAGE_ID_FN).ToString();
                string score = scoreDoc.Score.ToString(); 
                i++;

                passage_text = GetHighlight(query, passage_text);
                results.Add(new Result() { rank = rank.ToString(), title = title, url = url, passage_text = passage_text });

                // Output the result to a text file
                SaveResults.Add("Q0 "+ passage_id + " " + rank + " " + score + " " + " " + "N9573950_N9800174_N9858598_N8554544_Team 666");
            }

            // Return and display the results
            return results;
        }
        /// <summary>
        /// Get the highlight text in red，<font color='red'>", "</font>
        /// </summary>
        /// <param name="query"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        string GetHighlight(Query query, string text)
        {
            var scorer = new QueryScorer(query);
            var formatter = new SimpleHTMLFormatter("<font color='red'>", "</font>");
            var highlighter = new Highlighter(formatter, scorer);
            highlighter.TextFragmenter = new SimpleFragmenter(250);
            var stream = new StandardAnalyzer(VERSION).TokenStream("bodyText", new StringReader(text));
            return highlighter.GetBestFragments(stream, text, 3, "...");
        }

        /// <summary>
        /// Passage Preprocess
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        string PreProcess(string text, bool ps)
        {
            string processed = "";
            char[] splits = new char[] { ' ', '\t', '\'', '"', '-', '(', ')', ',', '’', '\n', ':', ';', '?', '.', '!' };
            string[] tokens = text.ToLower().Split(splits, StringSplitOptions.RemoveEmptyEntries);
            PorterStemmer porterStemmer = new PorterStemmer();
            foreach (string token in tokens)
            {
                if ((!stopWords.Contains(token)) && (token.Length > 2))
                {
                    if(ps)
                        processed += porterStemmer.stemTerm(token) + " ";
                    else
                        processed += token + " ";
                }
            }
            return processed;
        }
    }
}
