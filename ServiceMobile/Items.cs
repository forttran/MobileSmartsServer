using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceMobile
{
    internal class Items
    {
        public string Productkey;
        public string ProductMarking;
        public string ProductId;
        public string ProductName;
        public string ProductDate;
        public string ProductPartion;
        public string ProductCode;
        public double CurrentQuantity;
        public string Weight;
        public string Otg;
        public Items(
          string ProductMarking,
          string ProductId,
          string ProductName,
          string ProductDate,
          string ProductPartion,
          string ProductCode,
          double CurrentQuantit,
          string Weight,
          string Otg)
        {
            this.Productkey = ProductId + ";" + ProductDate;
            this.ProductMarking = ProductMarking;
            this.ProductId = ProductId;
            this.ProductName = ProductName;
            this.ProductDate = ProductDate;
            this.ProductPartion = ProductPartion;
            this.ProductCode = ProductCode;
            this.CurrentQuantity = CurrentQuantit;
            this.Weight = Weight;
            this.Otg = Otg;
        }
    }
}
