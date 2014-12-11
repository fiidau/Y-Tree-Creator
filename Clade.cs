using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace YTreeCreator
{
    public class Clade
    {
        List<string> snps = new List<string>();
        List<Clade> subclades = new List<Clade>();
        string file = null;
        static Crc16 crc = new Crc16();
        public static int ALLOWED_MISMATCHES_FOR_HG_WHILE_BUILDING = 1; //count

        public static List<string> REMOVED = new List<string>();

        public Clade(List<string> m,string m_file)
        {
            this.snps = m;
            this.file = m_file;
        }

        public bool addToClade(List<string> m_mutations,string new_file)
        {            
            List<string> common = snps.Intersect(m_mutations).ToList();
            if (common.Count == 0)
                return false;
            else
            {
                List<string> new_mutations1 = snps.Except(common).ToList();
                List<string> new_mutations2 = m_mutations.Except(common).ToList();
                snps = common;

                if (new_mutations1.Count <= ALLOWED_MISMATCHES_FOR_HG_WHILE_BUILDING)
                {
                    foreach (string mm in new_mutations1)
                        snps.Add(mm);
                    new_mutations1.Clear();
                }

                if (new_mutations2.Count <= ALLOWED_MISMATCHES_FOR_HG_WHILE_BUILDING)
                {
                    foreach (string mm in new_mutations1)
                        snps.Add(mm);
                    new_mutations2.Clear();
                }
                

                //

                bool flag = false;
                if (new_mutations1.Count > 0)
                {
                    foreach (Clade sc in subclades)
                    {
                        if (sc.addToClade(new_mutations1, Path.GetFileNameWithoutExtension(file)))
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                        subclades.Add(new Clade(new_mutations1, Path.GetFileNameWithoutExtension(file)));
                }

                if (new_mutations2.Count > 0)
                {
                    flag = false;
                    foreach (Clade sc in subclades)
                    {
                        if (sc.addToClade(new_mutations2, Path.GetFileNameWithoutExtension(new_file)))
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                        subclades.Add(new Clade(new_mutations2, Path.GetFileNameWithoutExtension(new_file)));
                }
                if (new_mutations1.Count != 0 || new_mutations2.Count != 0)
                    file = getHaplogroupName(snps);
                return true;
            }
        }

        private string getHaplogroupName(List<string> mutations)
        {
            /////// crc
            int val = 1;
            foreach (string m in mutations)
                val = val + crc.ComputeChecksum(m); //int.Parse(Regex.Replace(m, "[^0-9]", ""));

            return "HG" + val.ToString("X");
        }

        public string asXml()
        {
            if (snps.Count == 0)
                return "";
            IEnumerable<string> strings = snps;
            string joined = string.Join(",", strings.ToArray());
            string xml = "<Node HG='" + joined+"' Id='"+file+"'>\r\n";
            foreach (Clade c in subclades)
            {
                if (!REMOVED.Contains(c.file))
                {
                    xml += c.asXml();
                    xml += "\r\n";
                }
            }
            xml += "</Node>";
            return xml;
        }


        public string asHtml()
        {
            string html = "";
            if (snps.Count == 0)
                return "";
            IEnumerable<string> strings = snps;
            string joined = string.Join(",", strings.ToArray());

            if (subclades.Count > 0)
            {
                html = "<li><span class='wrapper sub-list'><i class='icon-plus-sign-alt expand-icon'></i><i class='icon-folder-open thumb-icon'></i><span><b>" + file + "</b> - <i>" + string.Join(", ", snps.ToArray()) + "</i></span></span>";              
                html+= "<ul>\r\n";

                foreach (Clade c in subclades)
                {
                    if (!REMOVED.Contains(c.file))
                    {
                        html += c.asHtml();
                        html += "\r\n";
                    }
                }
                html += "</ul></li>\r\n";
            }
            else
                html += "<li><span class='wrapper'><span><b>" + file + "</b> - <i>" + string.Join(", ", snps.ToArray()) + "</i></span></span></li>";

            return html;
        }
    }
}
