using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Able.ACC.DataAccess;
using Able.ACC.BusinessRules;
using System.Collections;
using Able.ACC.Common;
using Able.ACC.BusinessRules.WebSite;
using System.Text;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Able.Acc.Plugin;

public partial class ShowSystem_CA : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        UserInfo userInfo = new UserInfo();
        UserBL userBl = new UserBL();
        if (Able.Acc.Plugin.Module.IsHNDX)
        {
            HNDXLoginTicket ticket = this.Session[HNDXLoginTicket.SessionKey] as HNDXLoginTicket;
            bool flag = HNDXLoginTicket.Authenticate(ticket.LoginId, ticket.Ticket);
            if (flag)
            {
                userInfo = Able.Acc.Plugin.UserDA.LoginByLoginName(ticket.LoginId);
            }
        }
        else
        { 
            String userId = (String)Session["identityCard"];
            if (userId == null)
            {
                //DotNetCASClientServiceValidate client = new DotNetCASClientServiceValidate();
                //userId = client.Authenticate(Request, Response, false);  
                if (userId != "failed")
                {
                    Session["identityCard"] = userId;
                }
                // failed
            }
            userInfo = userBl.AuthenticateByUserIdentityCard(userId);
        }
        
        
        string url1 = Request.ApplicationPath + "/AdminSpace/Index.aspx?d=" + DateTime.Now.ToBinary();
        string url2 = Request.ApplicationPath + "/MySpace/MySpace.aspx?d=" + DateTime.Now.ToBinary();
        string url3 = Request.ApplicationPath + "/MySpace/MySpace.aspx?d=" + DateTime.Now.ToBinary();
        string url4 = "";
		
		
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

        Response.Redirect("CA1.aspx");
    }
}