using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.IO;
using RGiesecke.DllExport;
using System.Runtime.InteropServices;
namespace measure
{
    //要保证c#代码无错，如果运行出错，Sketchup会因为无法处理c#错误而直接闪退
    public class measure
    {
        // 下面的这行为新添加的代码，表示要导出函数，并指明导出函数名和调用约定
        [DllExport("readPoint", CallingConvention = CallingConvention.Cdecl)]
        public static string readPoint(string path)
        {

            if (path.Length == 0 || path == null || !File.Exists(path))
            {
                return "-1";
            }
            try
            {
                FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read);
                //文件为空
                if (fs.Length == 0)
                {
                    return "-1";
                }
                long offset;
                bool isAuth = false;
                string strTmp = "";
                //坐标值所在行信息
                string pointLine = "";
                //序列号所在行信息
                string serialLine = "";
                for (offset = fs.Length - 1; offset >= 0; offset--)
                {
                    fs.Seek(offset, SeekOrigin.Begin);
                    char c = Convert.ToChar(fs.ReadByte());
                    strTmp += c;
                    if (c == '\n' || c == '\r')
                    {
                        if (strTmp.Contains("X") && strTmp.Contains("Y") && strTmp.Contains("Z"))
                        {
                            pointLine = strTmp;
                        }
                        if (strTmp.Contains("LAIRES"))
                        {
                            //结束循环
                            serialLine = strTmp;
                            break;
                        }
                        strTmp = "";
                    }

                }
                fs.Close();
                //转换正常格式，读取得到每行数据为逆序字符串
                pointLine = reverseString(pointLine);
                serialLine = reverseString(serialLine);
                //去掉空白字符
                pointLine = Regex.Replace(pointLine, @"\s", "");
                serialLine = Regex.Replace(serialLine, @"\s", "");
                //获取X,Y,Z的位置
                int xIndex = pointLine.IndexOf("X");
                int yIndex = pointLine.IndexOf("Y");
                int zIndex = pointLine.IndexOf("Z");
                string x, y, z, serial;
                int pointLineLength = pointLine.Length;
                //根据xyz坐标值，截取字符串
                x = pointLine.Substring(xIndex + 1, yIndex - xIndex - 1);
                y = pointLine.Substring(yIndex + 1, zIndex - yIndex - 1);
                z = pointLine.Substring(zIndex + 1, pointLineLength - zIndex - 1);
                //serial单词中l字符所在位置
                int serialIndex = serialLine.IndexOf("SERIAL") + 5;
                int serialLineLength = serialLine.Length;
                serial = serialLine.Substring(serialIndex + 1, serialLineLength - serialIndex - 1);
                Console.WriteLine(serial);
                isAuth = authenticate(serial);
                if (isAuth)
                {
                    string point = x + @"," + y + @"," + z;
                    return point;
                }
                else
                {
                    return "-1";
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("An IO exception has been thrown!");
                Console.WriteLine(e.ToString());
                Console.ReadKey();
                return "-1";
            }

        }
        //字符串逆序
        public static string reverseString(string str)
        {
            char[] chars = str.ToCharArray();
            int begin = 0;
            int end = chars.Length - 1;
            char tempChar;
            while (begin < end)
            {
                tempChar = chars[begin];
                chars[begin] = chars[end];
                chars[end] = tempChar;
                begin++;
                end--;
            }
            string strResult = new string(chars);
            return strResult;
        }
        //判断是否认证
        public static bool authenticate(String serial)
        {

            if (serial != null)
            {
                String strPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                String file_path = strPath + @"\BFJO license\";

                if (Directory.Exists(file_path))
                {
                    String file = file_path + serial + @".lic";
                    if (File.Exists(file))
                    {
                        System.Text.Encoding.GetEncoding("gb2312");
                        String[] lines = File.ReadAllLines(file, System.Text.Encoding.Default); ;
                        String secret = @"www.scut.edu.cn//b8311";
                        String function = @"BFJO::House";
                        String str = lines[1] + lines[2] + lines[3] + function + secret;
                        string hexHash = SHA256(str);
                        if (lines[4] == hexHash)
                        {
                            return true;
                        }
                    }

                }
                else
                {
                    Console.WriteLine("license不存在");
                }

            }

            return false;
        }
        //SHA256加密
        public static string SHA256(string str)
        {
            byte[] bytValue = System.Text.Encoding.UTF8.GetBytes(str);
            try
            {
                SHA256 sha256 = new SHA256CryptoServiceProvider();
                byte[] retVal = sha256.ComputeHash(bytValue);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetSHA256HashFromString() fail,error:" + ex.Message);
            }
        }
    }

}
