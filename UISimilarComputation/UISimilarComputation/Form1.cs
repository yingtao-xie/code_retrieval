using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Diagnostics;

namespace UISimilarComputation
{
    struct sim_node
    {
        public double value;
        public int postion;
    }
    public partial class Form1 : Form
    {
        //全局变量
        String path = @"E:\Code_Source\测试结果\result\";//E:
        String file_name_address = "file_source.csv";
        double w_visual = 0.7;//w_structure = 1- w_visual
        bool printout_fact_switch = true;
        //double w_type = 0.5;//w_size = 1- w_type
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start(); //  计时开始
            //主程序
            ProccessData();
            ///////
            stopwatch.Stop(); //  计时结束
            TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
            double milliseconds = timespan.TotalMilliseconds;  //  总毫秒数
            label2.Text += milliseconds + "毫秒" + milliseconds / 60000 + "分钟";
            label3.Text += milliseconds / 6750 + "毫秒";
        }
        private void ProccessData()
        {
            String list1_name, list2_name;//读取的csv中的文件1和文件2的名字
            DataTable dt_files;
            double sim_Lin;
            //double sim_Reiss;
            //double sim_Our;
            
            //把list pair的内容保存在dt中
            dt_files = CSVtoDataTable(path + file_name_address);
            int len = dt_files.Rows.Count;
            for(int i = 0;i < len;i++)//跳过表头，从第二行开始(第一行,i=0)
            {
                //获取文件名称
                list1_name = dt_files.Rows[i][0].ToString();//Rows[i][0]表示第i行第1列
                list2_name = dt_files.Rows[i][1].ToString();//Rows[i][1]
                //计算相似度值
                //1
                //sim_Our = QueueSetSimOur(list1_name, list2_name, w_visual);//fact0.5
                printout_fact_switch = false;
                //2
                sim_Lin = QueueSetSimLin(list1_name, list2_name);
                //3
                //sim_Reiss = QueueSetSimReiss(list1_name, list2_name);
                //保存相似度值
                SaveSimValue(path, dt_files, i, sim_Lin);//sim_Our/sim_Reiss
            }
        } 
        //路径匹配算法
        //计算整个obj路径集合与待匹配的集合的相似性
        private double QueueSetSimOur(String file1_name, String file2_name, double w_visual)
        {
            //组装2个界面代码文件对应序列的完整路径
            String list1_path = path + @"UI_LIST\list_" + file1_name + ".csv";
            String list2_path = path + @"UI_LIST\list_" + file2_name + ".csv";
            //组装2个界面代码文件对应元素属性的完整路径
            String tree1_path = path + @"UI_TREE\" + file1_name + ".csv";
            String tree2_path = path + @"UI_TREE\" + file2_name + ".csv";
            List<String[]> list_queue1 = new List<String[]>();
            List<String[]> list_queue2 = new List<String[]>();
            //获取文件的序列
            list_queue1 = CSVtoStingList(list1_path);
            list_queue2 = CSVtoStingList(list2_path);
            int n = list_queue1.Count;
            int m = list_queue2.Count;
            var column = new List<sim_node[]>();
            if (n > m)//始终把路径少的集合作为列
            {
                //交换list_queue1,2   
                List<String[]> list_temp = new List<string[]>();
                list_temp = list_queue1;
                list_queue1 = list_queue2;
                list_queue2 = list_temp;
                //交换文件属性的路径
                String str_temp = tree1_path;
                tree1_path = tree2_path;
                tree2_path = str_temp;
                //交换m,n
                int num_temp;
                num_temp = n;
                n = m;
                m = num_temp;
            }
            for (int i = 0; i < n; i++)//矩阵列
            {
                String[] list_obj = list_queue1[i];
                //计算obj中的一条路径与待匹配的集合中的所有路径中最相似路径的相似度
                sim_node[] row = new sim_node[m];
                for (int j = 0; j < m; j++)//矩阵行
                {
                    String[] list_sub = list_queue2[j];
                    //计算结构相似性////////////////////////////
                    double med = SinglePathDistance(list_obj, list_sub, tree1_path, tree2_path);//使用med计算两路径的结构差异
                    //int lcs = LongestCommonSubsequence(list_obj, list_sub, tree1_path, tree2_path);//使用LCS计算两路径的相同结构
                    double structure_sim = 1 - med / Math.Max(list_obj.Length, list_sub.Length);
                    //double structure_sim = (double)lcs / (double)(med + lcs);
                    //求相对位置的差异作为位置影响因子
                    double loc_dif_fact = 1 - Math.Abs((double)(i + 1) / n - (double)(j + 1) / m);
                    structure_sim = structure_sim * loc_dif_fact;
                    //计算叶节点相似性//////////////////////////
                    double visual_sim = 0;
                    String obj = GetNodeType(tree1_path, list_obj[0])[0];//[0]返回type
                    String sub = GetNodeType(tree2_path, list_sub[0])[0];
                    if (obj.Equals(sub))//判断两条路径的叶节点的类型是否相等
                    {
                        visual_sim = 1;
                    }
                    //计算结构与叶节点共同的影响////////////////
                    row[j].value = structure_sim * (1 - w_visual) + visual_sim * w_visual;
                    row[j].postion = j;
                }
                SortColumn(row, m);//对该列排序
                column.Add(row);
            }
            //调试，排序前输出
            //PrintOut(column, m, n, label1);
            //找到整个集合每列不重叠的最大值,保证每列的第一个元素是最后的最大值
            for (int i = 1; i < n; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    //判断该列的最大值[i][0]是否与前列[j][0]最大值的行号重叠
                    if (column[j][0].postion == column[i][0].postion)
                    {
                        //如果重叠则比较其值的大小，小的则重排序列
                        if (column[j][0].value > column[i][0].value)
                        {
                            ReSortColumn(column[i],m);
                            i = 1;//一旦发生重排序，就重新开始循环
                        }
                        else
                        {
                            ReSortColumn(column[j], m);
                            i = 1;
                        }
                    }
                }
            }
            //调试，排序后输出
            //PrintOut(column, m, n, label4);
            //计算整体路径集合的相似度
            double sim = 0;
            for (int i = 1; i < n; i++)
            {
                sim = sim + column[i][0].value / n;//每条路径的相似度占总体相似度的1/n,n为路径条数
            }
            //label5.Text += sim;
            return sim;
        }

        //Reiss算法计算的相似度
        private double QueueSetSimReiss(String file1_name, String file2_name)
        {
            double sim = 0;
            //组装2个界面代码文件对应序列的完整路径
            String list1_path = path + @"UI_LIST\list_" + file1_name + ".csv";
            String list2_path = path + @"UI_LIST\list_" + file2_name + ".csv";
            //组装2个界面代码文件对应元素属性的完整路径
            String tree1_path = path + @"UI_TREE\" + file1_name + ".csv";
            String tree2_path = path + @"UI_TREE\" + file2_name + ".csv";
            List<String[]> list_queue1 = new List<String[]>();
            List<String[]> list_queue2 = new List<String[]>();
            //获取文件的序列
            list_queue1 = CSVtoStingList(list1_path);
            list_queue2 = CSVtoStingList(list2_path);
            int n = list_queue1.Count;
            int m = list_queue2.Count;
            ////////////////////
            for (int i = 0; i < n; i++)
            {
                String list_obj = list_queue1[i][0];
                //对sub每位设置一个是否匹配的flag_matched:默认未匹配
                bool[] flag_matched = new bool[m];
                for (int j = 0; j < m; j++)
                {
                    flag_matched[j] = false;
                }
                double flag = 0;//匹配是否成功
                for (int j = 0; j < m; j++)
                {
                    String list_sub = list_queue2[j][0];
                    String obj = GetNodeType(tree1_path, list_obj)[0];
                    String sub = GetNodeType(tree2_path, list_sub)[0];
                    if (!flag_matched[j])
                    {
                        if (obj.Equals(sub))//判断两条路径的叶节点是否相等
                        {
                            flag = 1;
                            flag_matched[j] = true;//设置为已匹配
                            break;
                        }
                        else
                        {
                            flag = 0;
                        }
                    }
                }
                sim = sim + (1 / (double)n) * flag;
            }            
            return sim;
        }
        //Lin算法计算的相似度
        private double QueueSetSimLin(String file1_name, String file2_name)
        {
            //double sim_lcs = 0;
            double sim_acs = 0;
            //组装2个界面代码文件对应序列的完整路径
            String list1_path = path + @"UI_LIST\list_" + file1_name + ".csv";
            String list2_path = path + @"UI_LIST\list_" + file2_name + ".csv";
            //组装2个界面代码文件对应元素属性的完整路径
            String tree1_path = path + @"UI_TREE\" + file1_name + ".csv";
            String tree2_path = path + @"UI_TREE\" + file2_name + ".csv";
            List<String[]> list_queue1 = new List<String[]>();
            List<String[]> list_queue2 = new List<String[]>();
            //获取文件的序列
            list_queue1 = CSVtoStingList(list1_path);
            list_queue2 = CSVtoStingList(list2_path);
            int n = list_queue1.Count;
            int m = list_queue2.Count;
            ////////////////////
            var column = new List<sim_node[]>();
            if (n > m)//始终把路径少的集合作为列
            {
                //交换list_queue1,2   
                List<String[]> list_temp = new List<string[]>();
                list_temp = list_queue1;
                list_queue1 = list_queue2;
                list_queue2 = list_temp;
                //交换文件属性的路径
                String str_temp = tree1_path;
                tree1_path = tree2_path;
                tree2_path = str_temp;
                //交换m,n
                int num_temp;
                num_temp = n;
                n = m;
                m = num_temp;
            }
            for (int i = 0; i < n; i++)//矩阵列
            {
                String[] list_obj = list_queue1[i];
                //计算obj中的一条路径与待匹配的集合中的所有路径中最相似路径的相似度
                sim_node[] row = new sim_node[m];
                for (int j = 0; j < m; j++)//矩阵行
                {
                    String[] list_sub = list_queue2[j];
                    //计算结构相似性////////////////////////////               
                    int acs = AllCommonSubsequence(list_obj, list_sub, tree1_path, tree2_path);
                    int acs_1 = AllCommonSubsequence(list_obj, list_obj, tree1_path, tree1_path);
                    int acs_2 = AllCommonSubsequence(list_sub, list_sub, tree2_path, tree2_path);
                    sim_acs = acs / Math.Sqrt(acs_1 * acs_2);
                    //int lcs = LongestCommonSubsequence(list_obj, list_sub, tree1_path, tree2_path);
                    //sim_lcs = lcs / Math.Sqrt(n * m);
                    row[j].value = sim_acs;
                    row[j].postion = j;
                }
                SortColumn(row, m);//对该列排序
                column.Add(row);
            }
            //调试，排序前输出
            //PrintOut(column, m, n, label1);
            //找到整个集合每列不重叠的最大值,保证每列的第一个元素是最后的最大值
            for (int i = 1; i < n; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    //判断该列的最大值[i][0]是否与前列[j][0]最大值的行号重叠
                    if (column[j][0].postion == column[i][0].postion)
                    {
                        //如果重叠则比较其值的大小，小的则重排序列
                        if (column[j][0].value > column[i][0].value)
                        {
                            ReSortColumn(column[i], m);
                            i = 1;//一旦发生重排序，就重新开始循环
                        }
                        else
                        {
                            ReSortColumn(column[j], m);
                            i = 1;
                        }
                    }
                }
            }
            //调试，排序后输出
            //PrintOut(column, m, n, label4);
            //计算整体路径集合的相似度
            double sim = 0;
            for (int i = 1; i < n; i++)
            {
                sim = sim + column[i][0].value / n;//每条路径的相似度占总体相似度的1/n,n为路径条数
            }
            //label5.Text += sim;
            return sim;
        }
        //获取节点类型
        private string[] GetNodeType(String tree_path, String node_num)
        {
            //type,width,height,orientation,level,child_num,father_id
            String[] attribution = new String[7];
            List<String[]> attribute_list = new List<String[]>();//树节点的类型列表
            //类型列表
            attribute_list = CSVtoStingList(tree_path);
            for (int i = 0; i < attribute_list.Count; i++)
            {
                if(attribute_list[i][0].Equals(node_num))//找到ID相等的元素，从而得到相应属性
                {
                    for (int j = 0;j < 7;j++)
                    {
                        attribution[j] = attribute_list[i][j+1];
                    }
                }
            }
            return attribution;
        }
        private double LowerOfThree(double first, double second, double third)
        {
            double min = Math.Min(first, second);
            return Math.Min(min, third);
        }
        //计算两条路径的SPD
        private double SinglePathDistance(String[] list1, String[] list2, String tree1_path, String tree2_path)
        {
            double[,] Matrix;
            int n = list1.Length;
            int m = list2.Length;
            int diff_leaf = 1;//标志为：叶子节点之间的类型差异
            int b1 = 0;//标志位：list1的非叶子节点的相邻层的划分方向是否一致
            int b2 = 0;//标志位：list2的非叶子节点的相邻层的划分方向是否一致
            double temp = 0;
            //attrib[0]:类型,attrib[3]:分割方向
            //attrib[5]:子节点个数
            //attrib[6]:父亲节点编号
            String[] attrib1 = new String[7];//当前节点的属性
            String[] attrib2 = new String[7];
            
            String[] attrib1_b = new String[7];//当前节点的前一节点的属性
            String[] attrib2_b = new String[7];

            int i = 0;
            int j = 0;
            if (n == 0)
            {
                return m;
            }
            if (m == 0)
            {
                return n;
            }
            Matrix = new double[n + 1, m + 1];
            for (i = 0; i <= n; i++)//初始化第一列
            {                
                Matrix[i, 0] = i;
            }
            for (j = 0; j <= m; j++)//初始化第一行
            {                
                Matrix[0, j] = j;
            }
            //叶子节点判断差异,相同设叶子差异为0，不同则为1
            attrib1 = GetNodeType(tree1_path, list1[0]);
            attrib2 = GetNodeType(tree2_path, list2[0]);
            if(attrib1[0] == attrib2[0])
            {
                diff_leaf = 0;//叶子节点类型相同
            }
            //非叶子节点计算路径距离
            for (i = 2; i <= n; i++)
            {
                attrib1 = GetNodeType(tree1_path, list1[i - 1]);//当前节点的属性                
                attrib1_b = GetNodeType(tree1_path, list1[i - 2]);//找前一节点的属性
                if (attrib1[3] == attrib1_b[3])//判断前后节点的划分方向是否一致
                {
                    b1 = 1;
                }
                else
                {
                    b1 = 0;
                }
                for (j = 2; j <= m; j++)
                {
                    attrib2 = GetNodeType(tree2_path, list2[j - 1]);//当前节点的属性                     
                    attrib2_b = GetNodeType(tree2_path, list2[j - 2]);//找前一节点的属性
                    if (attrib2[3] == attrib2_b[3])//判断前后节点的划分方向是否一致
                    {
                        b2 = 1;
                    }
                    else
                    {
                        b2 = 0;
                    }
                    if ((b1 == 0 && b2 == 1) || (b1 == 1 && b2 == 0))//待比较的其中一个节点与前节点划分相同，另一个节点不同
                    {                        
                        if (attrib1[3] == attrib2[3])
                        {
                            temp = 0.5;//1//0
                        }
                        else
                        {
                            temp = 1;
                        }
                    }
                    else if (b1 == 0 && b2 == 0)//待比较的两个节点与前节点划分都不相同
                    {
                        if (attrib1[3] == attrib2[3])//比较节点类型
                        {
                            if (attrib1[5] == attrib2[5])//比较子节点个数
                            {
                                temp = 0;
                            }
                            else
                            {
                                temp = 0.5;//1//0
                            }
                        }
                        else
                        {
                            if (attrib1[5] == attrib2[5])//比较子节点个数
                            {
                                temp = 0.5;//1//0
                            }
                            else
                            {
                                temp = 1;
                            }
                        }
                    }
                    else//(b1 == 1 && b2 == 1)//待比较的两个节点与前节点划分都相同
                    {
                        if (attrib1[3] == attrib2[3])//只比较节点划分方向
                        {
                            temp = 0;
                        }
                        else
                        {
                            temp = 1;
                        }
                    }
                    //特殊情况：该节点的子节点为1,则划分方向对其不影响，对任何匹配节点都算匹配。
                    if (attrib1[5] == "1" || attrib2[5] == "1")
                    {
                        temp = 0;
                    }
                    if (temp == 0)
                    {
                        Matrix[i, j] = LowerOfThree(Matrix[i - 1, j],
                                                    Matrix[i, j - 1],
                                                    Matrix[i - 1, j - 1]);
                    }
                    else
                    {
                        Matrix[i, j] = LowerOfThree(Matrix[i - 1, j] + 1,
                                                    Matrix[i, j - 1] + 1,
                                                    Matrix[i - 1, j - 1] + temp);
                    }
                }
            }
            return Matrix[n, m] + diff_leaf;
        }
        //计算两条路径的最长公共子序列
        private int LongestCommonSubsequence(String[] list1, String[] list2, String tree1_path, String tree2_path)
        {
            String val1;
            String val2;
            if (list1.Length == 0 || list2.Length == 0)
                return 0;
            int len = Math.Max(list1.Length, list2.Length);
            int[,] subsequence = new int[len + 1, len + 1];
            for (int i = 0; i < list1.Length; i++)
            {
                for (int j = 0; j < list2.Length; j++)
                {
                    val1 = GetNodeType(tree1_path, list1[i])[0];
                    val2 = GetNodeType(tree1_path, list2[j])[0];
                    if(val1.Equals(val2))//if (list1[i].Equals(list2[j]))
                        subsequence[i + 1, j + 1] = subsequence[i, j] + 1;
                    else
                        subsequence[i + 1, j + 1] = 0;
                }
            }
            int maxSubquenceLenght = (from sq in subsequence.Cast<int>() select sq).Max<int>();
            return maxSubquenceLenght;
        }
        //计算两条路径的公共子序列
        private int AllCommonSubsequence(String[] list1, String[] list2, String tree1_path, String tree2_path)
        {
            String val1;
            String val2;
            int n = list1.Length;
            int m = list2.Length;
            int[,] N= new int[n + 1,m + 1];
            //矩阵赋初值
            for (int i = 0; i <= n; i++)
            {
                N[i, 0] = 1;
            }
            for (int j = 0; j <= m; j++)
            {
                N[0, j] = 1;
            }
            //计算矩阵
            for (int i = 1; i <= n; i++)
            {
                val1 = GetNodeType(tree1_path, list1[i-1])[0];
                for (int j = 1; j <= m; j++)
                {
                    val2 = GetNodeType(tree1_path, list2[j-1])[0];
                    if(val1.Equals(val2))
                    {
                        N[i,j] = N[i-1,j-1] * 2; 
                    }
                    else{
                        N[i,j] = N[i-1,j] + N[i,j-1] - N[i-1,j-1];
                    }
                }
            }
            return N[n,m];
        }
        //对一列matrix_point排序(冒泡):最大的排最前
        private void SortColumn(sim_node[] column, int m)
        {
            for (int i = 0; i < m - 1; i++)
            {
               bool isSorted = true; //假设剩下的元素已经排序好了
               for(int j=0; j < m - 1 - i; j++)
               {
                   if (column[j].value < column[j + 1].value)
                    {
                       sim_node temp = column[j];
                       column[j] = column[j + 1];
                       column[j + 1] = temp;                        
                       isSorted = false; //一旦需要交换数组元素，就说明剩下的元素没有排序好
                    }
               }
               if(isSorted) break; //如果没有发生交换，说明剩下的元素已经排序好了
            }
        }
        //对该列重排序，第一放最后，其他依次上升
        private void ReSortColumn(sim_node[] column, int m)
        {
            sim_node temp = column[0];
            for (int i = 0; i < m - 1; i++)
            {
                column[i] = column[i + 1];
            }
            column[m - 1] = temp;
        }
        //调试，输出单一序列集合对比结果
        private void PrintOut(List<sim_node[]> column, int m, int n, Label label_x)
        {
            //调试，排序前输出
            if (printout_fact_switch)
            {
                for (int j = 0; j < m; j++)
                {
                    label_x.Text += "\n";
                    for (int i = 0; i < n; i++)
                    {
                        label_x.Text += column[i][j].postion + "    ";
                    }
                    label_x.Text += "\n";
                    for (int i = 0; i < n; i++)
                    {
                        label_x.Text += (column[i][j].value).ToString("f2") + " ";
                    }
                }
            }
        }
        //保存结果
        private void SaveSimValue(string filePath, DataTable dt_files,int i,
                                    double val_our
                                    )
        {
            FileStream fs;
            StreamWriter sw;
            String line;
            filePath = filePath + "result.csv";
            if (File.Exists(filePath)) //存档文件存在 
            {
                fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);
                sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                line = dt_files.Rows[i][0].ToString() + "," +
                       dt_files.Rows[i][1].ToString() + "," + val_our.ToString();
                sw.WriteLine(line);
                sw.Flush();//清空缓冲区
                sw.Close();//关闭流
                fs.Close();//关闭文件
            }
        }
        //读取CSV到数组
        public static List<String[]> CSVtoStingList(string filePath)
        {
            List<String[]> list_queue = new List<String[]>();
            System.Text.Encoding encoding = GetType(filePath); //Encoding.ASCII;//
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, encoding);
            String strLine = sr.ReadLine();
            while ((strLine = sr.ReadLine()) != null)
            {
                String[] node_queue = strLine.Split(',');
                list_queue.Add(node_queue);
            }
            //foreach (string i in node_queue)
            //{
            //    int temp = int.parse(i);//转换为int
            //}
            return list_queue;
        }
        //读取CSV到DataTable
        public static DataTable CSVtoDataTable(string filePath)
        {
            System.Text.Encoding encoding = GetType(filePath); //Encoding.ASCII;//
            DataTable dt = new DataTable();
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs, encoding);
            //记录每次读取的一行记录
            string strLine = "";
            //记录每行记录中的各字段内容
            string[] aryLine = null;
            string[] tableHead = null;
            //标示列数
            int columnCount = 0;
            //标示是否是读取的第一行
            bool IsFirst = true;
            //逐行读取CSV中的数据
            while ((strLine = sr.ReadLine()) != null)
            {
                if (IsFirst == true)
                {
                    tableHead = strLine.Split(',');
                    IsFirst = false;
                    columnCount = tableHead.Length;
                    //创建列
                    for (int i = 0; i < columnCount; i++)
                    {
                        DataColumn dc = new DataColumn(tableHead[i]);
                        dt.Columns.Add(dc);
                    }
                }
                else
                {
                    aryLine = strLine.Split(',');
                    DataRow dr = dt.NewRow();
                    for (int j = 0; j < columnCount; j++)
                    {
                        dr[j] = aryLine[j];
                    }
                    dt.Rows.Add(dr);
                }
            }
            if (aryLine != null && aryLine.Length > 0)
            {
                dt.DefaultView.Sort = tableHead[0] + " " + "asc";
            }
            sr.Close();
            fs.Close();
            return dt;
        }
        /// 给定文件的路径，读取文件的二进制数据，判断文件的编码类型
        /// <param name="FILE_NAME">文件路径</param>
        /// <returns>文件的编码类型</returns>
        public static System.Text.Encoding GetType(string FILE_NAME)
        {
            System.IO.FileStream fs = new System.IO.FileStream(FILE_NAME, System.IO.FileMode.Open,
                System.IO.FileAccess.Read);
            System.Text.Encoding r = GetType(fs);
            fs.Close();
            return r;
        }
        /// 通过给定的文件流，判断文件的编码类型
        /// <param name="fs">文件流</param>
        /// <returns>文件的编码类型</returns>
        public static System.Text.Encoding GetType(System.IO.FileStream fs)
        {
            byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM
            System.Text.Encoding reVal = System.Text.Encoding.Default;

            System.IO.BinaryReader r = new System.IO.BinaryReader(fs, System.Text.Encoding.Default);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
            {
                reVal = System.Text.Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                reVal = System.Text.Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                reVal = System.Text.Encoding.Unicode;
            }
            r.Close();
            return reVal;
        }
        /// 判断是否是不带 BOM 的 UTF8 格式
        /// <param name="data"></param>
        /// <returns></returns>
        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1;　 //计算当前正分析的字符应还有的字节数
            byte curByte; //当前分析的字节.
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X　
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式");
            }
            return true;
        }
    }
}
