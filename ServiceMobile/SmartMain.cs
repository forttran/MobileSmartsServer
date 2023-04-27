using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Cleverence;
using Cleverence.Common.SSL.SSLConfig;
using Cleverence.Warehouse;
using NLog;
using NLog.Filters;
using NLog.Fluent;
using NLog.Web;

namespace ServiceMobile{
	internal class SmartMain {
		public static StorageConnector connector;//коннектор к mobile smarts
		
		public static string docPath;//сетевой путь до расшаренной папки
		public static string smartServer;//имя базы mobile smarts
		public static string sqlServer;//коннект к sql server

		public static List<Items> itemsList = new List<Items>();//буфер
		public static List<string> addDocuments = new List<string>();//Список уже загруженных документов
        
		public static Logger currentClassLogger= NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        
		public static void doc_print(List<Items> itemsList) {//печать списка продуктов в документе
            currentClassLogger.Debug("______________________");
            foreach (Items items in itemsList)
                currentClassLogger.Debug(items.ProductMarking + ";" + items.ProductId + ";" + items.ProductName + ";" + items.ProductDate + ";" + items.ProductPartion + ";" + items.ProductCode + ";" + items.CurrentQuantity.ToString() + ";" + items.Weight + ";" + items.Otg);
        }
		
		public static double GetDays(DateTime start, DateTime end) {//количество дней между датами
			return (end - start).TotalDays;
		}
		
		public static string GetPath(Document doc) {//Получить имя файла
			string docPath = SmartMain.docPath;//сетевой путь
			string device = doc.GetField("idDevice").ToString();//id устройства
			string otg = doc.GetField("otg").ToString();//Отгрузка или инвентаризация
			string fileName = "inv_" + device + "_" + doc.CreateDate.ToString();//Собираем имя файла
			string newfileName = fileName.Replace("@", string.Empty).Replace("-", string.Empty).Replace(' ', '_').Replace(".", string.Empty).Replace(":", string.Empty) + ".csv";
			
			string path;
			string folder;
			
			if (otg == "False")
				folder = fileName.Replace("@", string.Empty).Replace("-", string.Empty).Split('_')[1];
			else
				folder = "OTG";

			path = docPath + folder + "\\" + newfileName;//Собираем весь путь
			currentClassLogger.Debug("folder=" + folder);
			currentClassLogger.Debug("Новый файл:" + path);
			currentClassLogger.Debug("newfileName=" + newfileName);
			return path;
		}
		public static void GetProductsInv(Document doc) {//Собираем спосок продукции в буфер после инвентаризации
			try {
				string otg = doc.GetField("otg").ToString();
				foreach (DocumentItem item in doc.GetDeclaredItems()) {
					try {
						currentClassLogger.Debug("item.ProductMarking=" + item.ProductMarking);
						currentClassLogger.Debug("item.ProductId=" + item.ProductId);
						currentClassLogger.Debug("item.ProductName=" + item.ProductName);
						currentClassLogger.Debug("item.GetField(data)=" + item.GetField("data").ToString());
						currentClassLogger.Debug("item.GetField(partion)=" + item.GetField("partion").ToString());
						currentClassLogger.Debug("item.GetField(code)=" + item.GetField("code").ToString());
						currentClassLogger.Debug("item.CurrentQuantity=" + item.CurrentQuantity.ToString());
						currentClassLogger.Debug("item.Product.BasePacking.SelfWeight=" + item.Product.BasePacking.SelfWeight.ToString("0.####").Replace(',', '.'));
						currentClassLogger.Debug("otg=" + otg);
						Items items = itemsList.Find((Predicate<Items>)(x => x.Productkey == item.ProductId + ";" + item.GetField("data").ToString()));
						if (items != null) {
							items.CurrentQuantity += item.CurrentQuantity;
						} else {
							if (item.CurrentQuantity > 300000.0)
								item.CurrentQuantity = 0.0;
							itemsList.Add(new Items(item.ProductMarking, item.ProductId, item.ProductName, item.GetField("data").ToString(), item.GetField("partion").ToString(), item.GetField("code").ToString(), item.CurrentQuantity, item.Product.BasePacking.SelfWeight.ToString("0.####").Replace(',', '.'), otg));
						}
					} catch (Exception ex) {
						currentClassLogger.Debug("Ошибка конкретного продукта");
						currentClassLogger.Error(ex.Message);
					}
				}
			} catch (Exception ex) {
				currentClassLogger.Debug("Ошибка подготовки файла");
				currentClassLogger.Error(ex.Message);
			}
		}

		public static void GetProductsOtg() {//Собираем спосок продукции в буфер после отгрузки
			try {
				//Сортрируем наш список и удаляем дубли
				itemsList.Sort(delegate (Items us1, Items us2) { return us1.ProductMarking.CompareTo(us2.ProductMarking); });
				Items prevItems = null;
				for (int i = 0; i < itemsList.Count; ++i) {
					if (prevItems == null) {
						prevItems = itemsList[i];
					} else {
						if (prevItems.ProductMarking == itemsList[i].ProductMarking) {
							if (itemsList[i].CurrentQuantity == 0)
								itemsList.RemoveAt(i);
							else
								itemsList.RemoveAt(i - 1);
						}
						prevItems = itemsList[i];
					}
				}
				doc_print(itemsList);
			} catch (Exception e) {
				currentClassLogger.Debug("Ошибка в этом коде");
				currentClassLogger.Error(e.Message);
				doc_print(itemsList);
			}
		}

		public static void GenerateFile(Document doc) {//Собственно генерируем файл
			string path = GetPath(doc);//Путь для файла
			string caption;
			caption = "Articul;ProductId;ProductName;ProductDate;ProductBatch;BarCode;ProductQuantity;Weight;otg";
			try {
				if (!File.Exists(path) && (itemsList.Count > 0)) {
					using (FileStream fileStream = File.Open(path, FileMode.Create)) {
						using (StreamWriter streamWriter = new StreamWriter((Stream)fileStream, Encoding.UTF8)) {
							streamWriter.WriteLine(caption);
							foreach (Items items in itemsList) {
								string str12 = items.ProductMarking + ";" + items.ProductId + ";" + items.ProductName + ";" + items.ProductDate + ";" + items.ProductPartion + ";" + items.ProductCode + ";" + items.CurrentQuantity.ToString() + ";" + items.Weight + ";" + items.Otg;
								streamWriter.WriteLine(str12);
							}
						}
					}
				} else {
					currentClassLogger.Debug("Документ пустой, фаил не создан");
				}
			} catch (Exception e) {
				currentClassLogger.Error("Ошибка непосредственной записи в файл");
				currentClassLogger.Error(e.Message);
			}
		}

		public static void RemoveDocument(Document doc) {//Удаляем документ если прошло время его резервного хранения
			DateTime now = DateTime.Now;
			DateTime CreateDoc;

			CreateDoc = doc.CreateDate;
			if (GetDays(CreateDoc, now) >= 0.5) {
				connector.RemoveDocument(doc.Id);
			}
			//currentClassLogger.Debug("data=" + doc.CreateDate.ToString() + "day=" + GetDays(CreateDoc, now));
		}
		public static void documents_to_file(){    //Главная функция выгрузки документов

            DocumentCollection documents = connector.GetDocuments("", false);//получаем список документов

			for (int index = 0; index < documents.Count; ++index){ //цикл всех документов
                
                itemsList.Clear();//очистка ,буфера
                Document doc = documents[index];//Получаем текущий документ
				
				if (!addDocuments.Any(n => n == doc.Name) && (int)doc.GetField("download") == 0) {//Мы его еще не выгружали
					string otg = doc.GetField("otg").ToString();
					try {
						if (otg == "True") {
							GetProductsOtg();//Получаем список на отгрузку
						} else {
							GetProductsInv(doc);//Получаем список инвентаризации
						}
						GenerateFile(doc);//Генерация файлика
					} catch (Exception e) {
						currentClassLogger.Error("Ошибка записи в файл");
						currentClassLogger.Error(e.Message);
					} finally {
						//connector.RemoveDocument(doc.Id);
						addDocuments.Add(doc.Name);
						currentClassLogger.Debug("Записали документ");
					}
				}
				RemoveDocument(doc);//Удаляем документ по прошествии 12 часов
			}
        }

        public static void work(){
            connector = new Cleverence.Warehouse.StorageConnector();
            //manager = new INIManager("config.ini");
            //manager.WritePrivateString("smartService", "sqlServer", "Data Source=SERVER4-MGN.m74.local;Initial Catalog=dfirm002_gmk;Persist Security Info=True;User ID=sa;Password=456123");
            //manager.WritePrivateString("smartService", "smartServer", "srvarc2-mgn.m74.local:10501/ccd5d4a5-8726-495f-bbad-dbed2192547a");
            //manager.WritePrivateString("smartService", "docPath", "\\\\srvarc2-mgn.m74.local\\docs\\");



            SmartMain.docPath = Properties.Settings.Default.docPath;
            SmartMain.smartServer = Properties.Settings.Default.smartServer;
            SmartMain.sqlServer = Properties.Settings.Default.sqlServer;

            try{
                currentClassLogger.Debug("connect");
                currentClassLogger.Debug("smartServer="+ smartServer);
                currentClassLogger.Debug("docPath=" + docPath);
                currentClassLogger.Debug("sqlServer=" + sqlServer);
                connector.SelectCurrentApp(smartServer);
            }catch(Exception e) {
                currentClassLogger.Error("Не могу подключиться:");
				currentClassLogger.Error(e.Message);
				Process.GetCurrentProcess().Kill();
            }
            while (true){
                documents_to_file();
                Thread.Sleep(10000);
            }
        }
    }
}
