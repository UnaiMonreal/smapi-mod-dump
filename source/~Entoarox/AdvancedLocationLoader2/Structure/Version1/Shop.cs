using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entoarox.AdvancedLocationLoader2.Structure.Version1
{
    class Shop
    {
        public string ShopId;
        public string[] ShopMessages;
        public int RestockDelay=0;
        public ShopItem[] Items=new ShopItem[0];
    }
}
