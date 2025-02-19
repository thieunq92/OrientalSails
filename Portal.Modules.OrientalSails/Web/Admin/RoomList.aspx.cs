using System;
using System.Collections;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using log4net;
using Portal.Modules.OrientalSails.Domain;
using Portal.Modules.OrientalSails.Web.UI;
using System.Linq;

namespace Portal.Modules.OrientalSails.Web.Admin
{
    public partial class RoomList : SailsAdminBase
    {
        #region -- Private Member --

        private readonly ILog _logger = LogManager.GetLogger(typeof (RoomList));
        private DateTime _date;

        private Cruise _cruise;
        protected Cruise ActiveCruise
        {
            get
            {
                if (_cruise==null && Request.QueryString["cruiseid"]!=null)
                {
                    _cruise = Module.CruiseGetById(Convert.ToInt32(Request.QueryString["cruiseid"]));
                }
                return _cruise;
            }
        }

        #endregion

        #region -- Page Event --

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                Title = Resources.titleRoomList;
                if (!IsPostBack)
                {
                    if (!string.IsNullOrEmpty(Request.QueryString["StartDate"]))
                    {
                        _date = DateTime.FromOADate(Convert.ToDouble(Request.QueryString["StartDate"]));
                        textBoxStartDate.Text = _date.ToString("dd/MM/yyyy");
                    }
                    GetDataSource();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error when Page_Load in RoomList", ex);
                ShowError(ex.Message);
            }
        }

        #endregion

        #region -- Private Method --

        private void GetDataSource()
        {
            if (_date != DateTime.MinValue)
            {
                var cruises = Module.CruiseGetAllNotLock(_date);
                rptRoom.DataSource = Module.RoomGetAllWithAvaiableStatus(ActiveCruise,_date, 2).Cast<Room>().Where(x=>cruises.Select(y=>y.Id).Contains(x.Cruise.Id));
            }
            else
            {
                var cruises = Module.CruiseGetAllNotLock(DateTime.Today);
                rptRoom.DataSource = Module.RoomGetAll(ActiveCruise).Cast<Room>().Where(x => cruises.Select(y => y.Id).Contains(x.Cruise.Id));
            }
            rptRoom.DataBind();
        }

        #endregion

        #region -- Control Event --

        protected void rptRoom_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            Room item = e.Item.DataItem as Room;
            if (item != null)
            {
                #region Name

                using (HyperLink hyperLink_Name = e.Item.FindControl("hyperLink_Name") as HyperLink)
                {
                    if (hyperLink_Name != null)
                    {
                        hyperLink_Name.Text = item.Name;
                        hyperLink_Name.NavigateUrl = string.Format(
                            "RoomEdit.aspx?NodeId={0}&SectionId={1}&RoomId={2}", Node.Id, Section.Id, item.Id);
                    }
                }

                #endregion

                #region Edit

                using (HyperLink hyperLinkEdit = e.Item.FindControl("hyperLinkEdit") as HyperLink)
                {
                    if (hyperLinkEdit != null)
                    {
                        hyperLinkEdit.NavigateUrl = string.Format("RoomEdit.aspx?NodeId={0}&SectionId={1}&RoomId={2}",
                                                                  Node.Id, Section.Id, item.Id);
                    }
                }

                #endregion

                #region RoomType

                using (Label label_RoomType = e.Item.FindControl("label_RoomType") as Label)
                {
                    if (label_RoomType != null)
                    {
                        label_RoomType.Text = item.RoomType.Name;
                    }
                }

                #endregion

                #region Room Class

                using (Label label_RoomClass=e.Item.FindControl("label_RoomClass") as Label)
                {
                    if (label_RoomClass!=null)
                    {
                        label_RoomClass.Text = item.RoomClass.Name;
                    }
                }
                #endregion

                Label labelCruise = e.Item.FindControl("labelCruise") as Label;
                if (labelCruise!=null)
                {
                    try
                    {
                        if (item.Cruise != null)
                        {
                            labelCruise.Text = item.Cruise.Name;
                        }
                    }
                    catch
                    {
                        item.Cruise = null;
                    }
                }

                if (_date != DateTime.MinValue)
                {
                    HtmlTableCell tdAvailable = e.Item.FindControl("tdAvailable") as HtmlTableCell;
                    if (tdAvailable!=null)
                    {
                        if (item.IsAvailable)
                        {
                            tdAvailable.InnerText = string.Format("{0} người lớn - {1} trẻ em - {2} trẻ sơ sinh", item.Adult, item.Child, item.Baby);
                        }
                        else
                        {
                            tdAvailable.InnerText = string.Format("{0} người lớn - {1} trẻ em - {2} trẻ sơ sinh", item.Adult, item.Child, item.Baby);
                            tdAvailable.Style[HtmlTextWriterStyle.BackgroundColor] = SailsModule.IMPORTANT;
                        }
                    }
                }
            }
        }

        protected void rptRoom_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            try
            {                
                Room item = Module.RoomGetById(Convert.ToInt32(e.CommandArgument));
                switch (e.CommandName)
                {
                    case "Delete":
                        Module.Delete(item);
                        GetDataSource();
                        break;
                    default :
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error when rptRoom_ItemCommand in RoomList", ex);
                ShowError(ex.Message);
            }
        }

        protected void buttonSearch_Click(object sender, EventArgs e)
        {
            if(IsValid)
            {
                if (!string.IsNullOrEmpty(textBoxStartDate.Text))
                {
                    DateTime date = DateTime.ParseExact(textBoxStartDate.Text, "dd/MM/yyyy",
                                                        CultureInfo.InvariantCulture);
                    PageRedirect(string.Format("RoomList.aspx?NodeId={0}&SectionId={1}&StartDate={2}",Node.Id,Section.Id,date.ToOADate()));
                }
            }
        }
        #endregion
    }
}