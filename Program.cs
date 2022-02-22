using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;
using System.Text;
using System.IO;
using System.Data;

namespace Homework_1
{
    class Crawler
    {
        static void Main(string[] args)
        {
            StartCrawler();
        }


        private static List<List<List<string>>> GetData(string html, List<List<List<string>>> resultDataset)
        {
            HtmlDocument doc = new HtmlDocument();
            Console.WriteLine("----Loading Page----");
            doc.LoadHtml(html);
            foreach (HtmlNode table in doc.DocumentNode.SelectNodes("//table[@class='table']/tbody/tr"))
            {
                var resultTable = new List<List<string>>();
                foreach (HtmlNode dataCell in table.SelectNodes("td"))
                {
                    var dataRow = new List<string>();
                    foreach (HtmlNode dataSpan in dataCell.SelectNodes("span"))
                    {
                        if (dataSpan.InnerText != null)
                        {
                            if (dataSpan.Attributes["class"].Value.ToString().Contains("outbound"))
                            {
                                dataRow.Add("Outbound");
                            }else if (dataSpan.Attributes["class"].Value.ToString().Contains("inbound"))
                            {
                                dataRow.Add("Inbound");
                            }else
                            {
                                dataRow.Add(dataSpan.InnerText);
                            }
                        }
                    }

                    resultTable.Add(dataRow);
                }

                resultDataset.Add(resultTable);
            }
            resultDataset.Add(new List<List<string>> { new List<string> {"end"} });
            return resultDataset;
        }


        private static List<string> CollectRow(List<List<string>> table)
        {
            var dataRow = new List<string>();
            foreach (var row in table)
            {
                for (int i = 0; i < row.Count; i++)
                {
                    if (row[i].CompareTo("Departs") == 0)
                    {
                        dataRow.Add(row[i + 2] + row[i + 1]);
                        int indexOfAirport = row[i + 3].IndexOf("(");
                        dataRow.Add(row[i + 3].Substring(indexOfAirport + 1, 3));
                    }
                    else if (row[i].CompareTo("Arrives") == 0)
                    {
                        dataRow.Add(row[i + 2] + row[i + 1]);
                        int indexOfAirport = row[i + 3].IndexOf("(");
                        dataRow.Add(row[i + 3].Substring(indexOfAirport + 1, 3));
                    }
                    else if (row[i].CompareTo("USD") == 0)
                    {
                        dataRow.Add(row[i + 1]);
                    }
                }
            }
            return dataRow;
        }


        private static List<string> MinRow(List<List<string>> table)
        {
            List<string> dataRow = new List<string>();
            int min = Int32.Parse(table[0][4]);
            int minID = 0;
            for (int i = 0; i < table.Count(); i++)
            {
                int temp = Int32.Parse(table[i][4]);
                if (min > temp)
                {
                    min = temp;
                    minID = i;
                }
            }
            dataRow = table[minID];
            return dataRow;
        }


        private static List<string> CheapestRoundTrip(List<List<string>> outboundRow, List<List<string>> inboundRow)
        {
            List<string> dataRow = new List<string>();
            dataRow.AddRange(MinRow(outboundRow));
            dataRow.AddRange(MinRow(inboundRow));
            return dataRow;
        }


        private static DataTable ListToDataTable(List<List<List<string>>> listOfFlights)
        {
            DataTable dataTableOfFlights = new DataTable();
            dataTableOfFlights.Columns.Add("Outbound Departure Date", typeof(string));
            dataTableOfFlights.Columns.Add("Outbound Departure Airport", typeof(string));
            dataTableOfFlights.Columns.Add("Outbound Arrival Date", typeof(string));
            dataTableOfFlights.Columns.Add("Outbound Arrival Airport", typeof(string));
            dataTableOfFlights.Columns.Add("Inbound Departure Date", typeof(string));
            dataTableOfFlights.Columns.Add("Inbound Departure Airport", typeof(string));
            dataTableOfFlights.Columns.Add("Inbound Arrival Date", typeof(string));
            dataTableOfFlights.Columns.Add("Inbound Arrival Airport", typeof(string));
            dataTableOfFlights.Columns.Add("Total Price", typeof(string));
            dataTableOfFlights.Columns.Add("Taxes", typeof(string));
            var dataRow = new List<string>();
            var outboundRow = new List<List<string>>();
            var inboundRow = new List<List<string>>();
            foreach (var table in listOfFlights)
            {
                if (table[0][0].CompareTo("Outbound") == 0)
                {
                    outboundRow.Add(CollectRow(table));
                }else if (table[0][0].CompareTo("Inbound") == 0)
                {
                    inboundRow.Add(CollectRow(table));
                }else if (table[0][0].CompareTo("end") == 0)
                {
                    dataRow = CheapestRoundTrip(outboundRow, inboundRow);
                    outboundRow = new List<List<string>>();
                    inboundRow = new List<List<string>>();
                    dataTableOfFlights.Rows.Add(dataRow[0], dataRow[1], dataRow[2], dataRow[3], dataRow[5], dataRow[6], dataRow[7], dataRow[8], "USD" + (Int32.Parse(dataRow[4]) + Int32.Parse(dataRow[9]) + 12).ToString(), "USD12");
                }
            }
            return dataTableOfFlights;
        }


        private static void DataTableToCSV(DataTable dataTable)
        {
            StringBuilder sb = new StringBuilder();

            string[] columnNames = dataTable.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName).
                                              ToArray();
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dataTable.Rows)
            {
                string[] fields = row.ItemArray.Select(field => field.ToString()).
                                                ToArray();
                for (int i = 0; i < fields.Count(); i++)
                {
                    string field = fields[i];
                    if (field.Contains(","))
                    {
                        int indexOfComma = field.IndexOf(",");
                        fields[i] = field.Remove(indexOfComma, 1);
                    }
                }
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText("test.csv", sb.ToString());
        }


        private static void StartCrawler()
        {
            DateTime localDate = DateTime.Now;
            List<string> urls = new List<string>();
            for (int i = 10; i < 21; i++)
            {
                DateTime departureDate = localDate.AddDays(i);
                DateTime returnDate    = localDate.AddDays(i+7);
                string url = "https://www.fly540.com/flights/nairobi-to-mombasa?isoneway=0&currency=USD&depairportcode=NBO&arrvairportcode=MBA&date_from="+ departureDate.ToString("ddd") + "%2C+"+ departureDate.Day.ToString() +"+"+ departureDate.ToString("MMM") +"+"+ departureDate.Year.ToString() + "&date_to=" + returnDate.ToString("ddd") + "%2C+" + returnDate.Day.ToString() + "+" + returnDate.ToString("MMM") + "+" + returnDate.Year.ToString() + "&adult_no=1&children_no=0&infant_no=0&searchFlight=&change_flight=";
                urls.Add(url);
            }
            WebClient webClient = new WebClient();
            List<List<List<string>>> results = new List<List<List<string>>>();
            foreach (var url in urls)
            {
                Console.WriteLine("----Downloading Page----");
                Console.WriteLine("[Page URL: " + url + "]");
                string page = webClient.DownloadString(url);
                results = GetData(page, results);
            }
            DataTableToCSV(ListToDataTable(results));
        }
    }
}