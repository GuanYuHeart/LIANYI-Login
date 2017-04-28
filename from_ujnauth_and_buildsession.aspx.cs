using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Able.ACC.BusinessRules;
using Able.ACC.DataAccess;
using Able.ACC.Common;
using System.Collections;
using Able.ACC.BusinessRules.WebSite;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

public partial class ShowSystem_from_ujnauth_and_buildsession : System.Web.UI.Page
{
    
    public static string connectionString = "";
    public static string colName = "";
    public static string tabName = "";
    public static int tea_start = 4;
    public static int tea_length = 8;
    public static int stu_start = 1;
    public static int stu_length = 11;

    public void getXML()
    {
            XmlDocument xml = new XmlDocument();
            xml.Load(HttpContext.Current.Server.MapPath("JiDaSql.config"));
            XmlNode str = xml.SelectSingleNode("consql");
            XmlNodeList xnlInfo = str.ChildNodes;
            foreach (XmlNode tempNode in xnlInfo)
            {
                if (tempNode.Name == "strSql")
                    connectionString = tempNode.Attributes["value"].Value;
                else if (tempNode.Name == "colName")
                    colName = tempNode.Attributes["value"].Value;
                else if(tempNode.Name == "tabName")
                    tabName = tempNode.Attributes["value"].Value;
                else if (tempNode.Name == "teaSub")
                {
                    tea_start = Convert.ToInt32(tempNode.Attributes["start"].Value);
                    tea_length = Convert.ToInt32(tempNode.Attributes["length"].Value);
                }
                else if (tempNode.Name == "stuSub")
                {
                    stu_start = Convert.ToInt32(tempNode.Attributes["start"].Value);
                    stu_length = Convert.ToInt32(tempNode.Attributes["length"].Value);
                }
            }
    }

    public static DataTable RunDataTableSQL(string strSQL, IDataParameter[] parameters)
    {
        DataTable dt = new DataTable();
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = strSQL;
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }
            }
        }
        return dt;
    }
    protected void Page_Load(object sender, EventArgs e)
    {
        string safekey = Request["safekey"];
       
        getXML();

        string StrSql = "select * from "+tabName+" where "+colName+"='" + safekey + "'";


        DataTable dt = RunDataTableSQL(StrSql, null);

        string url1 = Request.ApplicationPath + "/AdminSpace/Index.aspx?d=" + DateTime.Now.ToBinary();
        string url2 = Request.ApplicationPath + "/MySpace/MySpace.aspx?d=" + DateTime.Now.ToBinary();
        string url3 = Request.ApplicationPath + "/MySpace/MySpace.aspx?d=" + DateTime.Now.ToBinary();
        string url4 = "";
        
        string userNo = "";
        string identity = "";
        string LoginName = "";
        string Password = "";
        if (dt != null && dt.Rows.Count > 0)
        {
            string errcode = dt.Rows[0]["errcode"].ToString();
            switch (errcode)
            { 
                case "1":
                    Response.Write("为用户名密码错误");
                    break;
                case "2":
                    Response.Write("请将卡号密码输入完整");
                    break;
                case "3":
                    Response.Write("卡号密码必须为数字");
                    break;
                case "4":
                    Response.Write("卡号长度应为12位，密码的长度应为6位");
                    break;
                case "5":
                    Response.Write("卡号不存在");
                    break;
                case "10":
                    Response.Write("身份证号不正确");
                    break;
            }
            string yktkh = dt.Rows[0]["yktkhusername"].ToString();
            if (yktkh.Length > 5)
            {
                identity = yktkh.Substring(0, 1);
                if (identity == "0")
                {
                    identity = "3";
                    userNo = yktkh.Substring(tea_start,tea_length);
                }
                else
                {
                    identity = "2";
                    userNo = yktkh.Substring(stu_start, stu_length);
                }
            }


            DataTable dt2 = dtLoginNamAndPassword(userNo, identity);

            if (dt2 != null && dt2.Rows.Count > 0)
            {
                LoginName = dt2.Rows[0][0].ToString();
                Password = dt2.Rows[0][1].ToString();
            }

            UserBL userBl = new UserBL();
            UserInfo userInfo = userBl.CheckUser(LoginName, Password);
            if (userInfo != null && userInfo.IsRestrict == false)
            {
                Session.Add(Utilities.UserProfile, userInfo);
                // 身份(0超级管理员，1普通管理员,2学生，3教师，4系统外用户）
                Hashtable hsOnline = Application["OnlineUsers"] as Hashtable;
                if (hsOnline == null)
                {
                    hsOnline = new Hashtable();
                    Application["OnlineUsers"] = hsOnline;
                }
                if (hsOnline[userInfo.UserID] == null)
                {
                    hsOnline.Add(userInfo.UserID, DateTime.Now);
                    Application["OnlineUsers"] = hsOnline;
                }
                string url = "";
                if (userInfo.Identity <= 1)
                {
                    url = url1;
                }
                if (userInfo.Identity == 2)
                {
                    url = url2;
                    /*Begin:201010290953guokaiju
                     * Type:add
                     * By:管理员给学生发消息无反应...
                     */

                    Able.ACC.BusinessRules.MySpace.Message.G2SGetAdminMessage(userInfo.UserID);
                    //End:201010290953guokaiju
                }
                if (userInfo.Identity == 3)
                {
                    url = url3;
                    /*Begin:201010290953guokaiju
                     * Type:add
                     * By:管理员给教师发消息无反应...
                     */
                    Able.ACC.BusinessRules.MySpace.Message.G2SGetAdminMessage(userInfo.UserID);
                    //End:201010290953guokaiju
                }
                if (userInfo.Identity == 4)
                {
                    url4 = Request.ApplicationPath + "/Template/View.aspx?action=view&courseType=0&courseId=" +
                           userInfo.fCCPWebSiteID;
                }
                Session["WebURL"] = url;

                string strLastTime = userInfo.LastLoginTime.Replace('-', '/');

                #region 添加日志

                string logContent = string.Format("【fUserID】{0}【fLoginName】{1}【fUserName】{2}【fIdentity】{3}",
                                                  userInfo.UserID, userInfo.LoginName, userInfo.UserName,
                                                  userInfo.Identity);
                LogAction logAction = LogAction.登录;
                UserLog.Add(logAction, LogOperateObjName.其他, userInfo.UserID, logContent, false);

                #endregion

                string UserName = userInfo.UserName;
                string UserURL = url;

                StringBuilder sb = new StringBuilder();
                sb.Append("<User>");
                sb.Append("<Name>");
                sb.Append(UserName);
                sb.Append("</Name><Url>");
                sb.Append(UserURL);
                sb.Append("</Url></User>");

                Session["USER_STATE"] = sb.ToString();
            }
            else
            {
                Session["USER_STATE"] = "<User/>";

                if (userInfo != null && userInfo.IsRestrict == true)
                {
                    Response.Write("用户被锁 请联系管理员");
                    Response.StatusCode = 403;
                }
                else
                {
                    Response.Write("用户名或密码错误");
                    Response.StatusCode = 403;
                }
            }
        }
         Response.Redirect("CA1.aspx");
    }

    /// <summary>
    /// 利用教工号或学号获得登陆的用户名和密码
    /// </summary>
    /// <param name="fNo">教工号</param>
    /// <param name="fIdentity">身份</param>
    /// <returns></returns>
    private DataTable dtLoginNamAndPassword(string fNo, string fIdentity)
    {
        SqlParameter[] para = new SqlParameter[]
        {
            new SqlParameter("@fNo",fNo),
            new SqlParameter("@fIdentity",fIdentity)
        };
        DataTable dt = Able.G2S.DAL.ReadSqlHelper.RunDataTableProcedure("GetUserLoginNameAndPasswordByNo", para);
        return dt;
    }
}
