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
using System.Windows.Forms;
namespace measure64
{
    public class Class1
    {
        [DllExport("readPoint", CallingConvention = CallingConvention.Cdecl)]
        public static string readPoint(string uft8_path)
        {
            string path = encodingConvert(uft8_path);
           
            string status = "false";
            FileStream fs;
            if (path.Length == 0 || path == null || !File.Exists(path))
            {
                return "false";
            }
            try
            {
                using (fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    //文件为空   
                    if (fs == null || fs.Length == 0)
                    {
                   
                        return "false";
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

                    Console.WriteLine(pointLine);
                    //Console.WriteLine(line);
                    string serial = serialLine.Split(new String[] { "SERIAL" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    string[] pointTmp = pointLine.Split(new String[] { "SERIAL", "X", "Y", "Z" }, StringSplitOptions.RemoveEmptyEntries);
                    isAuth = authenticate(serial);

                    if (isAuth)
                    {
                        string point = pointTmp[0] + @"," + pointTmp[1] + @"," + pointTmp[2];
                        status = point;
                    }
                }

            }
            catch (Exception e)
            {
                string error = e.Message;
                MessageBox.Show(error);
                return "false";
            }
            finally
            {

            }

            return status;
        }
        //字符串逆序
        public static string reverseString(string str)
        {
            try
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
            catch (Exception e)
            {
                return "false"; ;
            }

        }
        //判断是否认证
        public static bool authenticate(String serial)
        {
            try
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
                            String secret = @"b8311-2018-05-24-15:56";
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
            }
            catch (Exception ex)
            {
                throw new Exception("GetSHA256HashFromString() fail,error:" + ex.Message);
            }


            return false;
        }
        //SHA256加密
        public static string SHA256(string str)
        {

            try
            {
                byte[] bytValue = System.Text.Encoding.UTF8.GetBytes(str);
                SHA256 sha256 = new SHA256CryptoServiceProvider();
                byte[] retVal = sha256.ComputeHash(bytValue);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
        //utf-8转换为默认编码格式
        public static string encodingConvert(string utf8String)
        {
            byte[] buffer1 = Encoding.Default.GetBytes(utf8String);
            byte[] buffer2 = Encoding.Convert(Encoding.UTF8, Encoding.Default, buffer1, 0, buffer1.Length);
            string strBuffer = Encoding.Default.GetString(buffer2, 0, buffer2.Length);
            return strBuffer;
        }

    }
}
