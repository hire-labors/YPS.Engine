using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YPS.Engine.Core.Tools
{
    public static class Parsers
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullAddress"></param>
        /// <param name="streetBlk"></param>
        /// <param name="locality"></param>
        /// <param name="region"></param>
        /// <param name="postalCode"></param>
        public static void SplitAddresses(string fullAddress, ref string streetBlk, ref string locality, ref string region, ref string postalCode)
        {
            string[] addressArray = fullAddress.Split(',');
            int num = 2;
            for(int i = addressArray.Length - 1; i >= 0; i--)
            {
                switch(num)
                {
                    case 0:
                        {
                            streetBlk = addressArray[i] + " " + streetBlk;
                            break;
                        }

                    case 1:
                        {
                            streetBlk = addressArray[i];
                            break;
                        }
                    case 2:
                        {
                            string[] pc_region = addressArray[i].Split(' ');
                            for(int j = 0; j < pc_region.Length; j++)
                            {
                                string temp = pc_region[j].ToUpper();
                                bool postal = false;
                                try
                                {
                                    int a = Convert.ToInt32(temp);
                                    postal = true;
                                }
                                catch
                                {

                                }
                                if(pc_region[j] != temp && pc_region[j].Contains("[0-9]+") == false)
                                {
                                    locality += " " + pc_region[j];
                                }
                                else if(postal)
                                {
                                    postalCode = pc_region[j];
                                }
                                else
                                {
                                    region = pc_region[j];
                                }
                            }
                            break;
                        }

                }
                num--;
            }
        }

    }
}
