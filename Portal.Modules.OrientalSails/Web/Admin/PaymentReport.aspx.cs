using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using CMS.ServerControls;
using CMS.Web.Util;
using GemBox.Spreadsheet;
using log4net;
using NHibernate.Criterion;
using Portal.Modules.OrientalSails.Domain;
using Portal.Modules.OrientalSails.Web.UI;
using Portal.Modules.OrientalSails.Web.Util;
using CMS.Core.Domain;
using Portal.Modules.OrientalSails.BusinessLogic;
using Portal.Modules.OrientalSails.Utils;
using Portal.Modules.OrientalSails.Enums;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Portal.Modules.OrientalSails.BusinessLogic.Share;
using System.Text.RegularExpressions;

namespace Portal.Modules.OrientalSails.Web.Admin
{
    public partial class PaymentReport : SailsAdminBase
    {

        private readonly ILog _logger = LogManager.GetLogger(typeof(PaymentReport));
        private PaymentReportBLL paymentReportBLL;
        private UserBLL userBLL;
        private PermissionBLL permissionBLL;

        public PaymentReportBLL PaymentReportBLL
        {
            get
            {
                if (paymentReportBLL == null)
                    paymentReportBLL = new PaymentReportBLL();
                return paymentReportBLL;
            }
        }
        public UserBLL UserBLL
        {
            get
            {
                if (userBLL == null)
                    userBLL = new UserBLL();
                return userBLL;
            }
        }
        public PermissionBLL PermissionUtil
        {
            get
            {
                if (permissionBLL == null)
                    permissionBLL = new PermissionBLL();
                return permissionBLL;
            }
        }
        public User CurrentUser
        {
            get
            {
                return UserBLL.UserGetCurrent();
            }
        }
        public IList<Booking> Bookings
        {
            get
            {
                return PaymentReportBLL.BookingGetByRequestString(Request.QueryString, CurrentUser);
            }
        }

        private int _adult;
        private int _adultTransfer;
        private int _baby;
        private int _child;
        private int _childTransfer;
        private int _double;
        private double _paid;
        private double _paidBase;
        private double _receivable;
        private double _total;
        private double _totalVnd;
        private int _twin;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ControlLoadData();
                BookingLoadData();
            }
        }

        protected void Page_Unload(object sender, EventArgs e)
        {
            if (paymentReportBLL != null)
            {
                paymentReportBLL.Dispose();
                paymentReportBLL = null;
            }

            if (userBLL != null)
            {
                userBLL.Dispose();
                userBLL = null;
            }

            if (permissionBLL != null)
            {
                permissionBLL.Dispose();
                permissionBLL = null;
            }
        }

        protected void btnDisplay_Click(object sender, EventArgs e)
        {
            Response.Redirect(Request.Url.GetLeftPart(UriPartial.Path) + QueryStringBuildByCriterion());
        }

        protected void rptBookingList_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.DataItem is Booking)
            {
                Booking booking = (Booking)e.Item.DataItem;

                Label lblSalesInCharge = e.Item.FindControl("lblSalesInCharge") as Label;
                try
                {
                    if (booking.Agency.Sale == null)
                        lblSalesInCharge.Text = "Unbound Sales";
                    else
                        lblSalesInCharge.Text = booking.BookingSale.Sale.UserName;
                }
                catch { }

                HyperLink hlAgency = e.Item.FindControl("hlAgency") as HyperLink;
                try
                {
                    hlAgency.Text = booking.Agency.Name;
                    hlAgency.NavigateUrl = UrlGetByAgency(booking.Agency.Id);
                }
                catch (Exception) { }

                HyperLink hlCruise = e.Item.FindControl("hlCruise") as HyperLink;
                try
                {
                    hlCruise.Text = booking.Cruise.Name;
                    hlCruise.NavigateUrl = UrlGetByCruise(booking.Cruise.Id);
                }
                catch (Exception) { }

                Label label_NoOfAdult = e.Item.FindControl("label_NoOfAdult") as Label;
                if (label_NoOfAdult != null)
                {
                    label_NoOfAdult.Text = booking.Adult.ToString();
                }

                Label label_NoOfChild = e.Item.FindControl("label_NoOfChild") as Label;
                if (label_NoOfChild != null)
                {
                    label_NoOfChild.Text = booking.Child.ToString();
                }

                Label label_NoOfBaby = e.Item.FindControl("label_NoOfBaby") as Label;
                if (label_NoOfBaby != null)
                {
                    label_NoOfBaby.Text = booking.Baby.ToString();
                }


                Label label_NoOfTransferAdult = e.Item.FindControl("label_NoOfTransferAdult") as Label;
                Label label_NoOfTransferChild = e.Item.FindControl("label_NoOfTransferChild") as Label;
                if (label_NoOfTransferChild != null && label_NoOfTransferAdult != null)
                {
                    bool transfer = false;
                    foreach (ExtraOption service in booking.ExtraServices)
                    {
                        if (service.Id == 1)
                        {
                            transfer = true;
                            break;
                        }
                    }
                    if (transfer)
                    {
                        _adultTransfer += booking.Adult;
                        _childTransfer += booking.Child;
                        label_NoOfTransferAdult.Text = booking.Adult.ToString();
                        label_NoOfTransferChild.Text = booking.Child.ToString();
                    }
                    else
                    {
                        label_NoOfTransferAdult.Text = "0";
                        label_NoOfTransferChild.Text = "0";
                    }
                }

                _adult += booking.Adult;
                _child += booking.Child;
                _baby += booking.Baby;


                Label label_NoOfDoubleCabin = e.Item.FindControl("label_NoOfDoubleCabin") as Label;
                if (label_NoOfDoubleCabin != null)
                {
                    label_NoOfDoubleCabin.Text = booking.DoubleCabin.ToString();
                }

                Label label_NoOfTwinCabin = e.Item.FindControl("label_NoOfTwinCabin") as Label;
                if (label_NoOfTwinCabin != null)
                {
                    label_NoOfTwinCabin.Text = booking.TwinCabin.ToString();
                }

                _double += booking.DoubleCabin;
                _twin += booking.TwinCabin;

                Label label_TotalPrice = e.Item.FindControl("label_TotalPrice") as Label;
                Label label_TotalPriceVnd = e.Item.FindControl("label_TotalPriceVnd") as Label;

                if (booking.IsTotalUsd)
                {
                    if (booking.Value > 0)
                    {
                        label_TotalPrice.Text = booking.Value.ToString("#,###");
                    }
                    else
                    {
                        label_TotalPrice.Text = "0";
                    }
                    label_TotalPriceVnd.Text = "0";

                    if (booking.CurrencyRate == 0)
                    {
                        booking.CurrencyRate = Module.ExchangeGetByDate((DateTime.Now)).Rate;
                    }
                }
                else
                {
                    if (booking.Value > 0)
                    {
                        label_TotalPriceVnd.Text = booking.Value.ToString("#,###");
                    }
                    else
                    {
                        label_TotalPriceVnd.Text = "0";
                    }
                    label_TotalPrice.Text = "0";
                    if (booking.CurrencyRate <= 0)
                        booking.CurrencyRate = 1;
                }

                if (booking.IsTotalUsd)
                {
                    _total += booking.Value;
                }
                else
                {
                    _totalVnd += booking.Value;
                }
                _paid += booking.Paid;
                _paidBase += booking.PaidBase;
                _receivable += booking.MoneyLeft;

                HtmlTableRow trItem = e.Item.FindControl("trItem") as HtmlTableRow;
                if (trItem != null)
                {
                    string color = string.Empty;
                    if (booking.Agency != null && booking.Agency.PaymentPeriod != PaymentPeriod.Monthly)
                    {
                        trItem.Attributes.Add("class", "custom-warning");
                    }
                    if (booking.IsPaymentNeeded)
                    {
                        trItem.Attributes.Add("class", "important");
                    }
                    if (booking.IsPaid)
                    {
                        trItem.Attributes.Add("class", "good");
                    }
                    if (booking.Inspection)
                    {
                        trItem.Attributes.Add("class", "inspection");
                    }
                }
            }
            else
            {
                if (e.Item.ItemType == ListItemType.Footer)
                {
                    #region -- get control --

                    Label label_NoOfAdult = (Label)e.Item.FindControl("label_NoOfAdult");
                    Label label_NoOfChild = (Label)e.Item.FindControl("label_NoOfChild");
                    Label label_NoOfBaby = (Label)e.Item.FindControl("label_NoOfBaby");
                    Label label_NoOfDoubleCabin = (Label)e.Item.FindControl("label_NoOfDoubleCabin");
                    Label label_NoOfTwinCabin = (Label)e.Item.FindControl("label_NoOfTwinCabin");
                    Label label_NoOfTransferAdult = (Label)e.Item.FindControl("label_NoOfTransferAdult");
                    Label label_NoOfTransferChild = (Label)e.Item.FindControl("label_NoOfTransferChild");
                    Label label_TotalPrice = (Label)e.Item.FindControl("label_TotalPrice");
                    Label label_TotalPriceVnd = (Label)e.Item.FindControl("label_TotalPriceVnd");
                    Literal litPaid = (Literal)e.Item.FindControl("litPaid");
                    Literal litPaidBase = (Literal)e.Item.FindControl("litPaidBase");
                    Literal litReceivable = (Literal)e.Item.FindControl("litReceivable");

                    #endregion

                    #region -- set value --

                    label_NoOfAdult.Text = _adult.ToString();
                    label_NoOfChild.Text = _child.ToString();
                    label_NoOfBaby.Text = _baby.ToString();
                    label_NoOfDoubleCabin.Text = _double.ToString();
                    label_NoOfTwinCabin.Text = _twin.ToString();
                    label_NoOfTransferAdult.Text = _adultTransfer.ToString();
                    label_NoOfTransferChild.Text = _childTransfer.ToString();
                    label_TotalPrice.Text = _total.ToString("#,###");
                    label_TotalPriceVnd.Text = _totalVnd.ToString("#,###");
                    if (_paid > 0)
                    {
                        litPaid.Text = _paid.ToString("#,###");
                    }
                    else
                    {
                        litPaid.Text = "0";
                    }

                    litPaidBase.Text = _paidBase.ToString("#,0.#");

                    if (_receivable > 0)
                    {
                        litReceivable.Text = _receivable.ToString("#,###");
                    }
                    else
                    {
                        litReceivable.Text = "0";
                    }

                    #endregion
                }
            }
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            if (!PermissionUtil.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.ACTION_EXPORTCONGNO)
                && !PermissionUtil.UserCheckRole(CurrentUser.Id, (int)Roles.Administrator))
            {
                ShowError("You do not have permission to use this function!");
                return;
            }

            var bookings = Bookings;
            ReceivableExportByAgency(bookings);

        }

        protected void btnExportRevenue_Click(object sender, EventArgs e)
        {
            if (!PermissionUtil.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.ACTION_EXPORTREVENUE)
                && !PermissionUtil.UserCheckRole(CurrentUser.Id, (int)Roles.Administrator))
            {
                ShowError("You do not have permission to use this function!");
                return;
            }

            var bookings = Bookings;
            RevenueExportByCruise(bookings);
        }

        protected void btnExportRevenueBySale_Click(object sender, EventArgs e)
        {
            if (!PermissionUtil.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.ACTION_EXPORTREVENUEBYSALE)
            && !PermissionUtil.UserCheckRole(CurrentUser.Id, (int)Roles.Administrator))
            {
                ShowError("You do not have permission to use this function!");
                return;
            }

            var bookings = Bookings;
            RevenueExportBySales(bookings);
        }

        protected void ControlLoadData()
        {
            ddlSales.DataSource = UserBLL.UserGetByRole((int)Roles.Sales);
            ddlSales.DataTextField = "Username";
            ddlSales.DataValueField = "Id";
            ddlSales.DataBind();
            ddlSales.Items.Insert(0, new ListItem("Unbound Sales", "0"));
            ddlSales.Items.Insert(0, "-- Sales in charge --");

            ddlTrips.DataSource = PaymentReportBLL.TripGetAll();
            ddlTrips.DataTextField = "Name";
            ddlTrips.DataValueField = "Id";
            ddlTrips.DataBind();
            ddlTrips.Items.Insert(0, "-- Service --");

            if (Request.QueryString["ti"] != null)
            {
                ddlTrips.SelectedValue = Request.QueryString["ti"];
            }

            txtFrom.Text = DateTimeUtil.DateGetDefaultFromDate().ToString("dd/MM/yyyy");
            if (Request.QueryString["f"] != null)
            {
                txtFrom.Text = Request.QueryString["f"];
            }

            txtTo.Text = DateTimeUtil.DateGetDefaultToDate().ToString("dd/MM/yyyy");
            if (Request.QueryString["t"] != null)
            {
                txtTo.Text = Request.QueryString["t"];
            }

            if (Request.QueryString["an"] != null)
            {
                txtAgency.Text = Request.QueryString["an"];
            }

            if (Request.QueryString["bi"] != null)
            {
                txtBookingCode.Text = Request.QueryString["bi"];
            }

            if (Request.QueryString["si"] != null)
            {
                ddlSales.SelectedValue = Request.QueryString["si"];
            }
        }

        protected void BookingLoadData()
        {
            var currentUserHaveRole = PermissionUtil.UserCheckRole(CurrentUser.Id, (int)Roles.Administrator);
            var currentUserHavePermisson = PermissionUtil.UserCheckPermission(CurrentUser.Id,
                (int)PermissionEnum.VIEW_ALLBOOKINGRECEIVABLE);
            var nvcQueryString = new NameValueCollection(Request.QueryString);
            if (!currentUserHaveRole && !currentUserHavePermisson)
            {
                var salesId = CurrentUser.Id.ToString();
                nvcQueryString.Remove("si");
                nvcQueryString.Add("si", salesId);
            }

            rptBookingList.DataSource = Bookings;
            rptBookingList.DataBind();
        }

        public void ReceivableExportByAgency(IList<Booking> bookings)
        {
            ExcelFile excelFile = new ExcelFile();
            excelFile.LoadXls(Server.MapPath("/Modules/Sails/Admin/ExportTemplates/CongNo.xls"));

            var exportDate = ExportGetExportDate();

            var agencies = (from b in bookings select b.Agency).Distinct().ToList<Agency>();
            foreach (Agency agency in agencies)
            {
                var sheetName = string.Format("{0} ({1})",
                    agency.Name.Count() + agency.Id.ToString().Count() + 3 >= 31 ? agency.Name.Substring(0, 31 - 3 - agency.Id.ToString().Count())
                    : agency.Name, agency.Id);
                sheetName = Regex.Replace(sheetName, @"[:\\/?*\[\]]", "");
                excelFile.Worksheets.AddCopy(sheetName, excelFile.Worksheets[0]);
            }

            foreach (Agency agency in agencies)
            {
                foreach (ExcelWorksheet sheet in excelFile.Worksheets)
                {
                    var splitString = sheet.Name.Split('(', ')');
                    if (splitString.Count() <= 1)
                    {
                        continue;
                    }
                    int agencyId = -1;
                    try
                    {
                        agencyId = Int32.Parse(splitString[splitString.Count() - 2]);
                    }
                    catch { }

                    if (agency.Id == agencyId)
                    {
                        var agencyBookings = (from b in Bookings
                                              where b.Agency == agency && b.IsPaid != true
                                              select b).ToList<Booking>();
                        ExcelWorksheet activeWorkSheet = sheet;
                        activeWorkSheet.Cells["F1"].Value = exportDate;
                        activeWorkSheet.Cells["C5"].Value = agency.Accountant + " " + agency.Name;
                        activeWorkSheet.Cells["C6"].Value = agency.Address;
                        activeWorkSheet.Cells["C7"].Value = agency.Phone;
                        activeWorkSheet.Cells["G7"].Value = agency.Fax;
                        activeWorkSheet.Cells["N21"].Value = CurrentUser.FullName;
                        activeWorkSheet.Cells["N22"].Value = CurrentUser.Website;
                        ReceivableExportByAgencyToSheet(activeWorkSheet, agencyBookings);
                    }
                }

            }

            if (excelFile.Worksheets.Count > 0)
            {
                excelFile.Worksheets[0].Delete();
            }

            ExcelSendBackToClient(excelFile, string.Format("CongNo{0}.xls", exportDate));
        }

        public void ReceivableExportByAgencyToSheet(ExcelWorksheet sheet, IList<Booking> agencyBookings)
        {
            int firstrow = 12;
            if (agencyBookings.Count > 0)
            {
                sheet.Rows[firstrow].InsertCopy(agencyBookings.Count - 1, sheet.Rows[firstrow]);
            }
            int activeRow = 12;
            var index = 1;

            foreach (Booking booking in agencyBookings)
            {
                IList _policies;
                if (booking.Agency != null && booking.Agency.Role != null)
                {
                    _policies = Module.AgencyPolicyGetByRole(booking.Agency.Role);
                }
                else
                {
                    _policies = Module.AgencyPolicyGetByRole(Module.RoleGetById(4));
                }

                sheet.Cells[activeRow, 0].Value = index;
                index++;

                sheet.Cells[activeRow, 1].Value = string.Format("OS{0:00000}", booking.Id);
                if (!string.IsNullOrEmpty(booking.AgencyCode))
                {
                    sheet.Cells[activeRow, 1].Value = booking.AgencyCode;
                }

                if (booking.Booker != null)
                {
                    sheet.Cells[activeRow, 2].Value = booking.Booker.Name;
                }

                sheet.Cells[activeRow, 3].Value = booking.StartDate;
                sheet.Cells[activeRow, 4].Value = booking.EndDate;

                sheet.Cells[activeRow, 5].Value = booking.Trip.TripCode + booking.TripOption;
                if (booking.Trip.NumberOfOptions <= 1)
                {
                    sheet.Cells[activeRow, 5].Value = booking.Trip.TripCode;
                }

                if (booking.ExtraServices.Select(x => x.Id).Contains(Module.ExtraOptionGetById(SailsModule.TRANSFER).Id))
                {
                    sheet.Cells[activeRow, 6].Value = "Yes";
                }
                else
                {
                    sheet.Cells[activeRow, 6].Value = "No";
                }


                string roomname = booking.RoomName.Replace("<br/>", "\n");
                if (roomname.Length > 0)
                {
                    roomname = roomname.Remove(roomname.Length - 1);
                }

                sheet.Cells[activeRow, 7].Value = roomname;
                sheet.Cells[activeRow, 8].Value = booking.Adult;
                sheet.Cells[activeRow, 9].Value = booking.Child;

                double unitprice;
                try
                {
                    Room room = Module.RoomGetById(SailsModule.DOUBLE);
                    unitprice = BookingRoom.Calculate(room.RoomClass, room.RoomType, 1, 0, false,
                                                             booking.Trip, booking.Cruise, booking.TripOption,
                                                             booking.StartDate, Module, _policies, ChildPrice,
                                                             AgencySupplement, booking.Agency);
                }
                catch (PriceException)
                {
                    unitprice = 0;
                }
                foreach (ExtraOption service in booking.ExtraServices)
                {
                    if (service.Deleted)
                    {
                        continue;
                    }

                    if (service.IsIncluded && !booking.ExtraServices.Select(x => x.Id).Contains(service.Id))
                    {
                        unitprice = unitprice - service.Price;
                    }

                    if (!service.IsIncluded && booking.ExtraServices.Select(x => x.Id).Contains(service.Id))
                    {
                        unitprice = unitprice + service.Price;
                    }
                }

                sheet.Cells[activeRow, 10].Value = unitprice;

                if (booking.CurrencyRate == 0)
                {
                    if (booking.IsTotalUsd)
                    {
                        booking.CurrencyRate = Module.ExchangeGetByDate((DateTime.Now)).Rate;
                    }
                }

                if (booking.IsTotalUsd == false && booking.CurrencyRate <= 0)
                {
                    booking.CurrencyRate = 1;
                }

                if (booking.IsTotalUsd)
                {
                    sheet.Cells[activeRow, 11].Value = Math.Round(booking.MoneyLeft / booking.CurrencyRate);
                }
                else
                {
                    sheet.Cells[activeRow, 12].Value = Math.Round(booking.MoneyLeft / booking.CurrencyRate);
                }
                sheet.Cells[activeRow, 13].Value = booking.CurrencyRate;
                sheet.Cells[activeRow, 14].Value = booking.MoneyLeft;

                activeRow++;
            }
        }

        public void RevenueExportAllCruiseReport(ExcelWorksheet sheet, IList<Booking> bookings)
        {
            RevenueExportByCruiseToSheet(sheet, bookings);
        }

        public void RevenueExportByCruise(IList<Booking> bookings)
        {
            ExcelFile excelFile = new ExcelFile();
            excelFile.LoadXls(Server.MapPath("/Modules/Sails/Admin/ExportTemplates/BaoCaoDoanhThu.xls"));

            var exportDate = ExportGetExportDate();
            ExcelWorksheet activeWorkSheet = excelFile.Worksheets[0];
            activeWorkSheet.Cells["G1"].Value = exportDate;
            var cruises = (from b in bookings select b.Cruise).Distinct().ToList<Cruise>();
            foreach (Cruise cruise in cruises)
            {
                excelFile.Worksheets.AddCopy(cruise.Name, excelFile.Worksheets[0]);

            }
            /* Chỉ active 1 sheet duy nhất khi open file */
            excelFile.Worksheets.Remove("TrashSheet");
            /* -- */
            RevenueExportAllCruiseReport(activeWorkSheet, bookings);
            foreach (Cruise cruise in cruises)
            {
                foreach (ExcelWorksheet sheet in excelFile.Worksheets)
                {
                    if (cruise.Name == sheet.Name)
                    {
                        activeWorkSheet = sheet;
                        var cruiseBookings = (from b in Bookings
                                              where b.Cruise == cruise
                                              select b).ToList<Booking>();
                        RevenueExportByCruiseToSheet(activeWorkSheet, cruiseBookings);
                    }
                }
            }
            ExcelSendBackToClient(excelFile, string.Format("DoanhThuTau {0}.xls", exportDate));
        }

        public void RevenueExportByCruiseToSheet(ExcelWorksheet sheet, IList<Booking> cruiseBookings)
        {
            double _totalUSD = 0;
            double _totalVND = 0;
            double _totalAll = 0;

            foreach (Booking booking in cruiseBookings)
            {
                if (booking.CurrencyRate == 0)
                {
                    if (booking.IsTotalUsd)
                    {
                        booking.CurrencyRate = Module.ExchangeGetByDate((DateTime.Now)).Rate;
                    }
                }

                if (booking.IsTotalUsd == false && booking.CurrencyRate <= 0)
                {
                    booking.CurrencyRate = 1;
                }

                if (booking.IsTotalUsd)
                {
                    _totalUSD += booking.Value;
                }
                else
                {
                    _totalVND += booking.Value;
                }

                _totalAll += (booking.Value * booking.CurrencyRate);
            }

            sheet.Cells["K9"].Value = _totalUSD;
            sheet.Cells["L9"].Value = _totalVND;
            sheet.Cells["N9"].Value = _totalAll;
            sheet.Cells["M16"].Value = CurrentUser.FullName;

            int firstrow = 7;
            sheet.Rows[firstrow].InsertCopy(cruiseBookings.Count - 1, sheet.Rows[firstrow]);

            int activeRow = 7;
            foreach (Booking booking in cruiseBookings)
            {
                IList _policies;
                if (booking.Agency != null && booking.Agency.Role != null)
                {
                    _policies = Module.AgencyPolicyGetByRole(booking.Agency.Role);
                }
                else
                {
                    _policies = Module.AgencyPolicyGetByRole(Module.RoleGetById(4));
                }


                if (booking.BookingSale.Sale != null)
                {
                    sheet.Cells[activeRow, 0].Value = booking.BookingSale.Sale.FullName;
                }
                sheet.Cells[activeRow, 1].Value = string.Format("OS{0:00000}", booking.Id);
                if (!string.IsNullOrEmpty(booking.AgencyCode))
                {
                    sheet.Cells[activeRow, 1].Value = booking.AgencyCode;
                }
                sheet.Cells[activeRow, 2].Value = SailsModule.NOAGENCY;
                if (booking.Agency != null)
                {
                    sheet.Cells[activeRow, 2].Value = booking.Agency.Name;
                }

                sheet.Cells[activeRow, 3].Value = booking.StartDate;
                sheet.Cells[activeRow, 4].Value = booking.EndDate;
                sheet.Cells[activeRow, 5].Value = booking.Trip.TripCode + booking.TripOption;
                if (booking.Trip.NumberOfOptions <= 1)
                {
                    sheet.Cells[activeRow, 5].Value = booking.Trip.TripCode;
                }


                if (booking.ExtraServices.Select(x => x.Id).Contains(Module.ExtraOptionGetById(SailsModule.TRANSFER).Id))
                {
                    sheet.Cells[activeRow, 6].Value = "Yes";
                }
                else
                {
                    sheet.Cells[activeRow, 6].Value = "No";
                }

                sheet.Cells[activeRow, 7].Value = booking.Adult;
                sheet.Cells[activeRow, 8].Value = booking.Child;

                double unitprice;
                try
                {
                    Room room = Module.RoomGetById(SailsModule.DOUBLE);
                    unitprice = BookingRoom.Calculate(room.RoomClass, room.RoomType, 1, 0, false,
                                                             booking.Trip, booking.Cruise, booking.TripOption,
                                                             booking.StartDate, Module, _policies, ChildPrice,
                                                             AgencySupplement, booking.Agency);
                }
                catch (PriceException)
                {
                    unitprice = 0;
                }
                foreach (ExtraOption service in booking.ExtraServices)
                {
                    if (service.Deleted)
                    {
                        continue;
                    }

                    if (service.IsIncluded && !booking.ExtraServices.Select(x => x.Id).Contains(service.Id))
                    {
                        unitprice = unitprice - service.Price;
                    }

                    if (!service.IsIncluded && booking.ExtraServices.Select(x => x.Id).Contains(service.Id))
                    {
                        unitprice = unitprice + service.Price;
                    }
                }

                sheet.Cells[activeRow, 9].Value = unitprice;

                if (booking.CurrencyRate == 0)
                {
                    if (booking.IsTotalUsd)
                    {
                        booking.CurrencyRate = Module.ExchangeGetByDate((DateTime.Now)).Rate;
                    }
                }

                if (booking.IsTotalUsd == false && booking.CurrencyRate <= 0)
                {
                    booking.CurrencyRate = 1;
                }

                if (booking.IsTotalUsd)
                {
                    sheet.Cells[activeRow, 10].Value = booking.Value;
                }
                else
                {
                    sheet.Cells[activeRow, 11].Value = booking.Value;
                }
                sheet.Cells[activeRow, 12].Value = booking.CurrencyRate;
                sheet.Cells[activeRow, 13].Value = booking.Value * booking.CurrencyRate;

                if (booking.MoneyLeft != 0)
                {
                    if (booking.Paid == 0 && booking.PaidBase == 0)
                    {
                        sheet.Cells[activeRow, 14].Value = "Unpaid";
                    }
                    else
                    {
                        sheet.Cells[activeRow, 14].Value = "Partly paid";
                    }
                }
                else
                {
                    sheet.Cells[activeRow, 14].Value = "Paid";
                    if (booking.PaidDate.HasValue)
                    {
                        sheet.Cells[activeRow, 14].Value = booking.PaidDate.Value;
                    }
                }
                activeRow++;
            }
        }

        public void RevenueExportBySales(IList<Booking> bookings)
        {
            ExcelFile excelFile = new ExcelFile();
            excelFile.LoadXls(Server.MapPath("/Modules/Sails/Admin/ExportTemplates/BaoCaoDoanhThuTheoSales.xls"));

            DateTime from = DateTimeUtil.DateGetDefaultFromDate();
            try
            {
                if (Request.QueryString != null)
                    from = DateTime.ParseExact(Request.QueryString["f"], "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch (Exception) { }

            DateTime to = DateTimeUtil.DateGetDefaultToDate();
            try
            {
                if (Request.QueryString != null)
                    to = DateTime.ParseExact(Request.QueryString["t"], "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch (Exception) { }

            var time = string.Format("{0:dd/MM/yyyy} - {1:dd/MM/yyyy}", from, to);
            if (from.Month == to.Month)
            {
                if (to.Subtract(from).Days == (DateTime.DaysInMonth(from.Year, from.Month) - 1))
                {
                    time = string.Format("{0:MMM - yyyy}", from);
                }
            }

            ExcelWorksheet activeWorkSheet = excelFile.Worksheets[0];
            activeWorkSheet.Cells["G1"].Value = time;
            var sales = (from b in bookings select b.BookingSale.Sale).Where(x => x != null).Distinct().ToList<User>();

            foreach (User _sales in sales)
            {
                excelFile.Worksheets.AddCopy(_sales.FullName, excelFile.Worksheets[0]);
            }

            foreach (User _sales in sales)
            {
                foreach (ExcelWorksheet sheet in excelFile.Worksheets)
                {
                    if (_sales.FullName == sheet.Name)
                    {
                        activeWorkSheet = sheet;
                        var salesBookings = (from b in Bookings
                                             where b.BookingSale.Sale == _sales
                                             select b).ToList<Booking>();
                        activeWorkSheet.Cells["C4"].Value = _sales.FullName;
                        RevenueExportBySalesToSheet(activeWorkSheet, salesBookings);
                    }
                }
            }

            if (excelFile.Worksheets.Count > 1)
            {
                excelFile.Worksheets[0].Delete();
            }

            ExcelSendBackToClient(excelFile, string.Format("DoanhThuSales {0}.xls", time));
        }

        public void RevenueExportBySalesToSheet(ExcelWorksheet sheet, IList<Booking> salesBookings)
        {
            double _totalUSD = 0;
            double _totalVnd = 0;
            int _adult = 0;
            int _child = 0;
            foreach (Booking booking in salesBookings)
            {
                if (booking.IsTotalUsd)
                {
                    _totalUSD += booking.Value;
                }
                else
                {
                    _totalVnd += booking.Value;
                }
                _adult += booking.Adult;
                _child += booking.Child;
            }

            sheet.Cells["G9"].Value = _adult;
            sheet.Cells["H9"].Value = _child;
            sheet.Cells["J9"].Value = _totalUSD;
            sheet.Cells["K9"].Value = _totalVnd;
            sheet.Cells["G16"].Value = UserIdentity.FullName;

            int firstRow = 7;
            sheet.Rows[firstRow].InsertCopy(salesBookings.Count - 1, sheet.Rows[firstRow]);

            int activeRow = firstRow;
            foreach (Booking booking in salesBookings)
            {
                IList _policies;
                if (booking.Agency != null && booking.Agency.Role != null)
                {
                    _policies = Module.AgencyPolicyGetByRole(booking.Agency.Role);
                }
                else
                {
                    _policies = Module.AgencyPolicyGetByRole(Module.RoleGetById(4));
                }

                if (booking.BookingSale.Sale != null)
                    sheet.Cells[activeRow, 0].Value = booking.BookingSale.Sale.FullName;

                sheet.Cells[activeRow, 1].Value = string.Format("OS{0:00000}", booking.Id);
                if (!string.IsNullOrEmpty(booking.AgencyCode))
                    sheet.Cells[activeRow, 1].Value = booking.AgencyCode;

                sheet.Cells[activeRow, 2].Value = "OrientalSails";
                if (booking.Agency != null)
                    sheet.Cells[activeRow, 2].Value = booking.Agency.Name;

                sheet.Cells[activeRow, 3].Value = booking.Trip.TripCode + booking.TripOption;
                if (booking.Trip.NumberOfOptions <= 1)
                    sheet.Cells[activeRow, 3].Value = booking.Trip.TripCode;


                sheet.Cells[activeRow, 4].Value = booking.StartDate;
                sheet.Cells[activeRow, 5].Value = booking.EndDate;

                sheet.Cells[activeRow, 6].Value = booking.Adult;
                sheet.Cells[activeRow, 7].Value = booking.Child;

                double unitprice;
                try
                {
                    Room room = Module.RoomGetById(SailsModule.DOUBLE);
                    unitprice = BookingRoom.Calculate(room.RoomClass, room.RoomType, 1, 0, false,
                                                      booking.Trip, booking.Cruise, booking.TripOption,
                                                      booking.StartDate, Module, _policies, ChildPrice,
                                                      AgencySupplement, booking.Agency);
                }
                catch
                {
                    unitprice = 0;
                }

                foreach (ExtraOption service in booking.ExtraServices)
                {
                    if (service.Deleted)
                    {
                        continue;
                    }

                    if (service.IsIncluded && !booking.ExtraServices.Select(x => x.Id).Contains(service.Id))
                    {
                        unitprice = unitprice - service.Price;
                    }

                    if (!service.IsIncluded && booking.ExtraServices.Select(x => x.Id).Contains(service.Id))
                    {
                        unitprice = unitprice + service.Price;
                    }
                }

                sheet.Cells[activeRow, 8].Value = unitprice;

                if (booking.CurrencyRate == 0)
                {
                    if (booking.IsTotalUsd)
                    {
                        booking.CurrencyRate = Module.ExchangeGetByDate((DateTime.Now)).Rate;
                    }
                }

                if (booking.IsTotalUsd == false && booking.CurrencyRate <= 0)
                {
                    booking.CurrencyRate = 1;
                }

                if (booking.IsTotalUsd)
                {
                    sheet.Cells[activeRow, 9].Value = booking.Value;
                }
                else
                {
                    sheet.Cells[activeRow, 10].Value = booking.Value;
                }

                activeRow++;
            }

        }

        public void ExcelSendBackToClient(ExcelFile excelFile, string fileName)
        {
            Response.Clear();
            Response.Buffer = true;
            Response.ContentType = "application/vnd.ms-excel";
            Response.AppendHeader("content-disposition", "attachment; filename=" + fileName);
            MemoryStream m = new MemoryStream();
            excelFile.SaveXls(m);
            Response.OutputStream.Write(m.GetBuffer(), 0, m.GetBuffer().Length);
            Response.OutputStream.Flush();
            Response.OutputStream.Close();
            m.Close();
            Response.End();
        }

        public string ExportGetExportDate()
        {
            DateTime from = DateTimeUtil.DateGetDefaultFromDate();
            try
            {
                if (Request.QueryString != null)
                    from = DateTime.ParseExact(Request.QueryString["f"], "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch (Exception) { }

            DateTime to = DateTimeUtil.DateGetDefaultToDate();
            try
            {
                if (Request.QueryString != null)
                    to = DateTime.ParseExact(Request.QueryString["t"], "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch (Exception) { }

            var date = string.Format("{0:dd_MM_yyyy}-{1:dd_MM_yyyy}", from, to);
            return date;
        }

        public string QueryStringBuildByCriterion()
        {
            NameValueCollection nvcQueryString = new NameValueCollection();
            nvcQueryString.Add("NodeId", "1");
            nvcQueryString.Add("SectionId", "15");

            string query = string.Empty;

            if (!string.IsNullOrEmpty(txtFrom.Text))
            {
                nvcQueryString.Add("f", txtFrom.Text);
            }

            if (!string.IsNullOrEmpty(txtTo.Text))
            {
                nvcQueryString.Add("t", txtTo.Text);
            }

            if (!string.IsNullOrEmpty(txtAgency.Text))
            {
                nvcQueryString.Add("an", txtAgency.Text);
            }

            if (ddlTrips.SelectedIndex > 0)
            {
                nvcQueryString.Add("ti", ddlTrips.SelectedValue);
            }

            if (ddlSales.SelectedIndex > 0)
            {
                nvcQueryString.Add("si", ddlSales.SelectedValue);
            }

            if (!string.IsNullOrEmpty(txtBookingCode.Text))
            {
                nvcQueryString.Add("bi", txtBookingCode.Text);
            }

            var criterions = (from key in nvcQueryString.AllKeys
                              from value in nvcQueryString.GetValues(key)
                              select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value))).ToArray();

            return "?" + string.Join("&", criterions);
        }

        public string QueryStringBuildByAgency(int agencyId)
        {

            NameValueCollection nvcQueryString = new NameValueCollection(Request.QueryString);
            nvcQueryString.Remove("ai");
            nvcQueryString.Add("ai", agencyId.ToString());

            var criterions = (from key in nvcQueryString.AllKeys
                              from value in nvcQueryString.GetValues(key)
                              select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value))).ToArray();

            return "?" + string.Join("&", criterions);
        }

        public string QueryStringBuildByCruise(int cruiseId)
        {

            NameValueCollection nvcQueryString = new NameValueCollection(Request.QueryString);
            nvcQueryString.Remove("ci");
            nvcQueryString.Add("ci", cruiseId.ToString());

            var criterions = (from key in nvcQueryString.AllKeys
                              from value in nvcQueryString.GetValues(key)
                              select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value))).ToArray();

            return "?" + string.Join("&", criterions);
        }

        public string UrlGetByAgency(int agencyId)
        {
            return Request.Url.GetLeftPart(UriPartial.Path) + QueryStringBuildByAgency(agencyId);
        }

        public string UrlGetByCruise(int cruiseId)
        {
            return Request.Url.GetLeftPart(UriPartial.Path) + QueryStringBuildByCruise(cruiseId);
        }
    }
}