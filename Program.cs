using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using System.Diagnostics;

namespace Triplets
{


    class Program
    {
        static void Main(string[] args)
        {
            DoWork();
            Console.WriteLine("Нажмите любую кнопку для завершения...");
            Console.ReadKey();
        }

        static void DoWork()
        {
            Stopwatch stopWatch = new Stopwatch();
            Console.WriteLine("Введите путь к тестовому файлу: ");
            string txtPath = Console.ReadLine().Trim();

            SyncClass.fileRead = false;
            SyncClass.StringsCounterReset();

            void ReadFile(object state)
            {
                using (var sr = new StreamReader(txtPath, Encoding.UTF8))
                {
                    while (!sr.EndOfStream)
                    {
                        StringList.Add(sr.ReadLine());
                        SyncClass.StringsCounterInc();
                        SyncClass.ThreadsCounterInc();
                    }
                }
                SyncClass.fileRead = true;
            }

            stopWatch.Start();

            ThreadPool.QueueUserWorkItem(ReadFile);

            while (!((SyncClass.StringsCounterGet() == 0)&&(SyncClass.fileRead)))
            {
                if (SyncClass.StringsCounterGet() > 0)
                {
                    ThreadPool.QueueUserWorkItem(Triplets.FindTriplets);
                    SyncClass.StringsCounterDec();
                }
            }

            while (SyncClass.GetWorkingThreads() != 0)
            { }

            var myList = Triplets.GetTriplets().ToList();
            myList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            int outputCount;
            if (myList.Count < 10) outputCount = myList.Count;
            else outputCount = 10;
            for (int i = 0; i < outputCount; i++)
            {
                Console.WriteLine("{0}:\"{1}\" - {2}", i + 1, myList[i].Key, myList[i].Value);
            }

            stopWatch.Stop();

            Console.WriteLine("Затрачено времени: {0} мс", stopWatch.ElapsedMilliseconds);

            Triplets.ClearTriplets();
        }

    }

    class Triplets
    {
        private static Dictionary<string, int> dicTriplets = new Dictionary<string, int>();

        public static Dictionary<string, int> GetTriplets()
        {
            return dicTriplets;
        }

        public static void ClearTriplets()
        {
            dicTriplets.Clear();
        }

        public static void AddTriplet (string newTriplet)
        {
            lock (SyncClass.dicLocker)
            {
                if (dicTriplets.ContainsKey(newTriplet))
                {
                    dicTriplets[newTriplet] += 1;
                }
                else
                {
                    dicTriplets.Add(newTriplet, 1);
                }
            }
        }

        public static void FindTriplets(object state)
        {
            string processedStr = "";
            string lettersStr;

            processedStr = StringList.Take();//.Trim().ToLower();

            if (processedStr != null)
            {
                processedStr = processedStr.Trim().ToLower();
                lettersStr = "abcdefghijklmnopqrstuvwxyzабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
                
                if (processedStr.Length >= 3)
                {
                    for (int i = 0; i < processedStr.Length - 2; i++)
                    {
                        if ((lettersStr.Contains(processedStr[i])) && (lettersStr.Contains(processedStr[i + 1])) && (lettersStr.Contains(processedStr[i + 2])))
                        {
                            string newTriplet = processedStr.Substring(i, 3);
                            Triplets.AddTriplet(newTriplet);
                        }
                    }
                }
            }
            SyncClass.ThreadsCounterDec();
        }
    }

    class StringList
    {
        private static List<string> list = new List<string>();
        
        public static void Add (string newString)
        { 
            list.Add(newString);
        }

        public static string Take()
        {
            
            lock (SyncClass.stringLocker)
            {
                string takenStr;
                try
                {
                    takenStr = list[0];
                    list.RemoveAt(0);
                }
                catch
                {
                    takenStr = "";
                }
                return takenStr;
            }
            
        }

        public static bool IsEmpty()
        {
            if (list.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }
    }

    class SyncClass
    {
        public static object stringLocker = new object();
        public static object dicLocker = new object();
        public static object threadcounterLocker = new object();
        public static object stringsCounterLocker = new object();
        public static bool fileRead = false;
        private static int stringsCounter = 0;
        private static int threadsWorking = 0;

        public static void ThreadsCounterInc()
        {
            lock (threadcounterLocker)

                threadsWorking += 1;

        }

        public static void ThreadsCounterDec()
        {
            lock (threadcounterLocker)
                threadsWorking -= 1;
        }

        public static int GetWorkingThreads()
        {
            return threadsWorking;
        }

        public static void StringsCounterInc()
        {
            lock (stringsCounterLocker)

                stringsCounter += 1;

        }

        public static void StringsCounterDec()
        {
            lock (stringsCounterLocker)
                stringsCounter -= 1;
        }

        public static int StringsCounterGet()
        {
            return stringsCounter;
        }

        public static void StringsCounterReset()
        {
            lock (stringsCounterLocker)
                stringsCounter = 0;
        }
    }
}
