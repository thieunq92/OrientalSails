using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Aspose.Words.Tables;
using CMS.Web.Util;
using GemBox.Spreadsheet;
using iTextSharp.text.pdf;
using log4net;
using NHibernate.Criterion;
using Portal.Modules.OrientalSails.Domain;
using Portal.Modules.OrientalSails.ReportEngine;
using Portal.Modules.OrientalSails.Web.UI;
using Portal.Modules.OrientalSails.Web.Util;
using TextBox = System.Web.UI.WebControls.TextBox;
using System.Linq;
using Portal.Modules.OrientalSails.BusinessLogic;
using System.Xml;
using System.Xml.Linq;
using OfficeOpenXml;
using Portal.Modules.OrientalSails.BusinessLogic.Share;
using CMS.Core.Domain;
using CMS.Web.UI;
using Portal.Modules.OrientalSails.Web.Admin.Utilities;
using Portal.Modules.OrientalSails.Enums;
using System.Threading.Tasks;

namespace Portal.Modules.OrientalSails.Web.Admin
{
    public partial class BookingReport : SailsAdminBasePage
    {
        public BookingReportBLL BookingReportBLL
        {
            get; set;
        }

        public UserBLL UserBLL
        {
            get; set;
        }

        public PermissionBLL PermissionBLL
        {
            get; set;
        }
        protected IList Suppliers
        {
            get; set;
        }

        protected IList Guides
        {
            get; set;
        }

        protected IList DailyCost
        {
            get; set;
        }
        /// <summary>
        /// Lấy tàu theo query string
        /// </summary>
        public Cruise Cruise { get; set; }

        /// <summary>
        /// Lấy ngày theo query string
        /// </summary>
        public DateTime Date
        {
            get; set;
        }

        public SailsTrip Trip { get; set; }

        public User CurrentUser
        {
            get; set;
        }

        public string CruiseIdAllow
        {
            get; set;
        }

        public LockingExpense LockingExpense
        {
            get; set;
        }

        public string LockingExpenseString
        {
            get; set;
        }
        public bool LockingExpenseBoolean
        {
            get; set;
        }
        /// <summary>
        /// Lấy danh sách các booking approved và pending theo ngày và tàu 
        /// </summary>
        public IEnumerable<Booking> ListBooking
        {
            get; set;
        }
        /// <summary>
        /// Lấy danh sách các tàu
        /// </summary>
        public IEnumerable<Cruise> ListCruise
        {
            get; set;
        }
        /// <summary>
        /// Kiểm tra tài khoản có được phép xem cột total trong bảng không
        /// </summary>
        public bool CanViewTotal
        {
            get; set;
        }
        /// <summary>
        /// Kiểm tra tài khoản có được phép xem cột total trong bảng không
        /// </summary>
        public bool CanViewAgency
        {
            get; set;
        }

        public bool CanViewSpecialRequestFood
        {
            get; set;
        }

        public bool CanViewSpecialRequestRoom
        {
            get; set;
        }

        public IList<IvRoleCruise> ListRoleCruises
        {
            get; set;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            this.Master.Title = "Booking by date";
            if (!IsPostBack)
            {
                txtDate.Text = !string.IsNullOrEmpty(Request.QueryString["Date"]) ? Request.QueryString["Date"] : DateTime.Today.ToString("dd/MM/yyyy");
            }
			BookingReportBLL = new BookingReportBLL();
            UserBLL = new UserBLL();
            PermissionBLL = new PermissionBLL();
            Date = DateTime.Today;
            if (DateTime.TryParseExact(txtDate.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateAsDateTime))
            {
                Date = dateAsDateTime;
            }

            CurrentUser = UserBLL.UserGetCurrent();
            CanViewTotal = PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.VIEW_TOTAL_BY_DATE);
            CanViewAgency = PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.VIEW_TOTAL_BY_DATE);
            CanViewSpecialRequestFood = PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.SPECIALREQUEST_FOOD);
            CanViewSpecialRequestRoom = PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.SPECIALREQUEST_ROOM);

            ListCruise = Module.CruiseGetAllNotLock(Date, CurrentUser).OrderBy(x => x.Id);
            DailyCost = Module.CostTypeGetDailyInput();
            Guides = Module.GuidesGetAll();
            Suppliers = Module.SupplierGetAll();

            ListRoleCruises = Module.CruisePermissionsGetByUser(UserIdentity);

            if (int.TryParse(Request.QueryString["cruiseid"], out int cruiseId))
            {
                Cruise = BookingReportBLL.CruiseGetById(cruiseId);
            }

            if (int.TryParse(Request.QueryString["tripid"], out int tripId))
            {
                Trip = BookingReportBLL.TripGetById(tripId);
            }

            RegisterAsyncTask(new PageAsyncTask(PageLoadAsync));
        }


        private async Task PageLoadAsync()
        {
           
            await Task.Run(() =>
            {
               
                LockingExpense = BookingReportBLL.LockingExpenseGetAllByCriterion(Date).List().FirstOrDefault();           

                if (LockingExpense != null)
                {
                    LockingExpenseString = "true";
                }

                LockingExpenseString = "false";

                if (LockingExpense != null)
                {
                    LockingExpenseBoolean = true;
                }

                LockingExpenseBoolean = false;

                CruiseIdAllow = String.Join(",", ListRoleCruises.Select(x => x.Cruise.Id).ToArray());


                //if (!UserIdentity.IsInRole("Operation") && !UserIdentity.HasPermission(AccessLevel.Administrator))
                //{
                //    btnSavePickupTime.Visible = false;
                //}
                btnSave.Enabled = !LockingExpenseBoolean;
                btnExportCustomerData.Visible = Cruise != null && Cruise.Id > 0;
                btnProvisionalRegister.Visible = Cruise != null && Cruise.Id > 0;
                btnExportXml.Visible = Cruise != null && Cruise.Id > 0;

                rptCruises.DataSource = ListCruise;
                rptCruises.DataBind();

                if (Cruise != null)
                {
                    rptTrips.DataSource = Cruise.Trips;
                    rptTrips.DataBind();
                }

            }, new System.Threading.CancellationToken());

        }

        protected void Page_Unload(object sender, EventArgs e)
        {
            if (BookingReportBLL != null)
            {
                BookingReportBLL.Dispose();
                BookingReportBLL = null;
            }
            if (UserBLL != null)
            {
                UserBLL.Dispose();
                UserBLL = null;
            }
            if (PermissionBLL != null)
            {
                PermissionBLL.Dispose();
                PermissionBLL = null;
            }
        }

        public void ShowWarning(string warning)
        {
            Session["WarningMessage"] = "<strong>Warning!</strong> " + warning + "<br/>" + Session["WarningMessage"];
        }

        public void ShowErrors(string error)
        {
            Session["ErrorMessage"] = "<strong>Error!</strong> " + error + "<br/>" + Session["ErrorMessage"];
        }

        public void ShowSuccess(string success)
        {
            Session["SuccessMessage"] = "<strong>Success!</strong> " + success + "<br/>" + Session["SuccessMessage"];
        }
        protected void btnDisplay_Click(object sender, EventArgs e)
        {
            string url = string.Format("BookingReport.aspx?NodeId={0}&SectionId={1}&Date={2}", Node.Id, Section.Id,
                                       Date.ToString("dd/MM/yyyy"));
            PageRedirect(url);
        }

        protected void rptCruises_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.DataItem is Cruise)
            {
                var cruise = (Cruise)e.Item.DataItem;
                HyperLink hplCruises = e.Item.FindControl("hplCruises") as HyperLink;
                hplCruises.CssClass = "btn btn-default";
                if (hplCruises != null)
                {
                    if (cruise.Id.ToString() == Request.QueryString["cruiseid"])
                    {
                        hplCruises.CssClass = "btn btn-default active";
                    }
                    var numberOfRoom = BookingReportBLL.BookingRoomGetRowCountByCriterion(cruise, Date);
                    var numberOfPax = BookingReportBLL.CustomerGetRowCountByCriterion(cruise, Date);
                    if (cruise.CruiseType == Enums.CruiseType.Cabin)
                    {
                        hplCruises.Text = string.Format("{0} ({1} pax/{2} cabins)", cruise.Name, numberOfPax.ToString(), numberOfRoom.ToString());
                    }
                    else if (cruise.CruiseType == Enums.CruiseType.Seating)
                    {
                        hplCruises.Text = string.Format("{0} ({1} pax/{2} seats)", cruise.Name, numberOfPax.ToString(), cruise.NumberOfSeat);
                    }

                    hplCruises.Attributes.Add("data-pax", numberOfPax.ToString());
                    hplCruises.NavigateUrl = string.Format(
                        "BookingReport.aspx?NodeId={0}&SectionId={1}&Date={2}&cruiseid={3}", Node.Id, Section.Id,
                        Date.ToString("dd/MM/yyyy"), cruise.Id);
                }
            }
            else
            {
                HyperLink hplCruises = e.Item.FindControl("hplCruises") as HyperLink;
                if (hplCruises != null)
                {
                    if (Request.QueryString["cruiseid"] == null)
                    {
                        hplCruises.CssClass = "btn btn-default active";
                    }

                    hplCruises.NavigateUrl = string.Format(
                        "BookingReport.aspx?NodeId={0}&SectionId={1}&Date={2}", Node.Id, Section.Id, Date.ToString("dd/MM/yyyy"));
                }
            }
        }

        protected void btnViewFeedback_Click(object sender, EventArgs e)
        {
            DateTime date = Date;
            string url = string.Format("FeedbackReport.aspx?NodeId={0}&SectionId={1}&from={2}&to={2}", Node.Id, Section.Id,
                                       date.ToOADate());
            PageRedirect(url);
        }
        protected void btnSave_Click(object sender, EventArgs e)
        {
            ShowSuccess("Saved successfully");
            Session["Redirect"] = true;
            var script = "angular.element(\"[ng-controller='bookingReportController']\").scope().save();";
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "initBtnSaveClicked", script, true);
        }
        protected void btnLockDate_Click(object sender, EventArgs e)
        {
            var lockingExpense = LockingExpense;
            if (lockingExpense == null)
            {
                lockingExpense = new LockingExpense();
            }
            lockingExpense.Date = Date;
            BookingReportBLL.LockingExpenseSaveOrUpdate(lockingExpense);
            ShowSuccess("Locked date successfully");
            Response.Redirect(Request.RawUrl);
        }
        protected void btnUnlockDate_Click(object sender, EventArgs e)
        {
            var lockingExpense = LockingExpense;
            if (lockingExpense == null)
            {
                Response.Redirect(Request.RawUrl);
            }
            BookingReportBLL.LockingExpenseDelete(lockingExpense);
            ShowSuccess("Unlocked date successfully");
            Response.Redirect(Request.RawUrl);
        }
        protected void btnExportCustomerData_Click(object sender, EventArgs e)
        {
            ListBooking = BookingReportBLL.BookingGetAllByByCriterion(CurrentUser, Date, Cruise, new List<StatusType>() { StatusType.Approved, StatusType.Pending });
            if (Cruise.Code != "VD")
            {
                var listCustomer = ListBooking.SelectMany(x => x.BookingRooms.SelectMany(y => y.RealCustomers));// Lấy danh sách khách hàng từ danh sách booking hiện tại
                MemoryStream mem = new MemoryStream();
                using (var excelPackage = new ExcelPackage(new FileInfo(Server.MapPath("/Modules/Sails/Admin/ExportTemplates/ClientDetails.xlsx"))))
                {
                    var sheet = excelPackage.Workbook.Worksheets["Client Details"];
                    sheet.Cells["G2"].Value = "Start date:" + " " + Date.ToString("dd-MMM");
                    sheet.Cells["H2"].Value = Cruise.Name;
                    int startRow = 5;
                    int currentRow = startRow;
                    int templateRow = startRow;
                    currentRow++;
                    sheet.InsertRow(currentRow, listCustomer.Count() - 1, templateRow);
                    currentRow--;
                    for (int i = 0; i < listCustomer.Count(); i++)
                    {
                        var customer = listCustomer.ElementAt(i) as Customer;
                        sheet.Cells[currentRow, 1].Value = i + 1;
                        sheet.Cells[currentRow, 2].Value = customer.Fullname.ToUpper();
                        sheet.Cells[currentRow, 4].Value = customer.Birthday.HasValue
                            ? customer.Birthday.Value.ToString("dd/MM/yyyy") : "";
                        sheet.Cells[currentRow, 3].Value = StringUtil.GetFirstLetter(customer.Gender);
                        if (customer.Nationality != null)
                            if (customer.Nationality.Name.ToLower() != "Khong ro")
                                sheet.Cells[currentRow, 6].Value = customer.Nationality.Name;
                            else
                                sheet.Cells[currentRow, 6].Value = customer.Passport;
                        sheet.Cells[currentRow, 5].Value = customer.Passport;
                        if (customer.Booking != null && customer.Booking.Cruise != null && customer.Booking.Trip != null)
                        {
                            sheet.Cells[currentRow, 7].Value = customer.Booking.Cruise.GetModifiedCruiseName() + " " + customer.Booking.Trip.NumberOfDay + "D";
                        }
                        currentRow++;
                    }
                    excelPackage.SaveAs(mem);
                }
                Response.Clear();
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = "";
                if (Cruise != null)
                {
                    fileName = string.Format("\"Client details - {0} - {1}.xlsx\"", Date.ToString("dd_MM_yyyy"), Cruise.Name.Replace(" ", "_"));
                }
                Response.AppendHeader("content-disposition", "attachment; filename=" + fileName);
                mem.Position = 0;
                byte[] buffer = mem.ToArray();
                Response.BinaryWrite(buffer);
                Response.End();
            }
            else
            {
                var listCustomer = ListBooking.SelectMany(x => x.Customers);// Lấy danh sách khách hàng từ danh sách booking hiện tại
                MemoryStream mem = new MemoryStream();
                using (var excelPackage = new ExcelPackage(new FileInfo(Server.MapPath("/Modules/Sails/Admin/ExportTemplates/DanhSachKhachTauNgay.xlsx"))))
                {
                    var sheet = excelPackage.Workbook.Worksheets["Client Details"];
                    sheet.Cells["C7"].Value = listCustomer.Count();
                    sheet.Cells["F7"].Value = listCustomer.Where(x => x.Nationality?.AbbreviationCode == "VNM").Count();
                    sheet.Cells["I7"].Value = listCustomer.Where(x => x.Nationality?.AbbreviationCode != "VNM").Count();
                    int startRow = 12;
                    int currentRow = startRow;
                    int templateRow = startRow;
                    currentRow++;
                    sheet.InsertRow(currentRow, listCustomer.Count() - 1, templateRow);
                    sheet.InsertRow(currentRow + listCustomer.Count() - 1, 10, templateRow);
                    currentRow--;
                    for (int i = 0; i < listCustomer.Count(); i++)
                    {
                        if (i > 0)
                        {
                            sheet.Cells[currentRow, 2, currentRow, 4].Merge = true;
                            sheet.Cells[currentRow, 6, currentRow, 7].Merge = true;
                            sheet.Cells[currentRow, 8, currentRow, 10].Merge = true;
                        }
                        var customer = listCustomer.ElementAt(i) as Customer;
                        sheet.Cells[currentRow, 1].Value = i + 1;
                        sheet.Cells[currentRow, 2].Value = customer.Fullname?.ToUpper() ?? "";
                        if (customer.IsMale != null && customer.IsMale.Value)
                        {
                            sheet.Cells[currentRow, 5].Value = customer.Birthday.HasValue
                            ? customer.Birthday.Value.ToString("yyyy") : "";
                        }
                        else
                        {
                            sheet.Cells[currentRow, 6].Value = customer.Birthday.HasValue
                            ? customer.Birthday.Value.ToString("yyyy") : "";
                        }

                        if (customer.Nationality != null)
                        {
                            sheet.Cells[currentRow, 8].Value = customer.Nationality.Name;
                        }
                        currentRow++;
                    }
                    excelPackage.SaveAs(mem);
                }
                Response.Clear();
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = "";
                if (Cruise != null)
                {
                    fileName = string.Format("\"Client details - {0} - {1}.xlsx\"", Date.ToString("dd_MM_yyyy"), Cruise.Name.Replace(" ", "_"));
                }
                Response.AppendHeader("content-disposition", "attachment; filename=" + fileName);
                mem.Position = 0;
                byte[] buffer = mem.ToArray();
                Response.BinaryWrite(buffer);
                Response.End();
            }
        }
        protected void btnProvisionalRegister_Click(object sender, EventArgs e)
        {
            if (Cruise.Name.Contains("Calypso"))
            {
                ProvisionalRegisterCalypsoCruise();
            }
            else
            {
                ProvisionalRegisterOtherCruise();
            }
        }
        private void ProvisionalRegisterCalypsoCruise()
        {
            var bookings = BookingReportBLL.BookingGetAllStartInDate(Date, Cruise);
            var customers = bookings.SelectMany(x => x.BookingRooms.SelectMany(y => y.Customers));
            MemoryStream mem = new MemoryStream();
            using (var excelPackage = new ExcelPackage(new FileInfo(Server.MapPath("/Modules/Sails/Admin/ExportTemplates/ProvisionalRegisterTemplate_Calypso.xlsx"))))
            {
                var sheet = excelPackage.Workbook.Worksheets["ProvisionalRegister"];
                sheet.Name = "PR-Calypso-" + Date.ToString("dd_MM_yyyy");
                int startRow = 2;
                int currentRow = startRow;
                int templateRow = startRow;
                currentRow++;
                sheet.InsertRow(currentRow, customers.Count() - 1, templateRow);
                currentRow--;
                for (int i = 0; i < customers.Count(); i++)
                {
                    var customer = customers.ElementAt(i) as Customer;
                    sheet.Cells[currentRow, 1].Value = i + 1;
                    sheet.Cells[currentRow, 2].Value = customer.Fullname?.ToUpper();
                    sheet.Cells[currentRow, 3].Value = customer.Birthday.HasValue
                        ? customer.Birthday.Value.ToString("dd/MM/yyyy") : "";
                    sheet.Cells[currentRow, 4].Value = customer.IsMale.HasValue ? (customer.IsMale.Value ? "Nam" : "Nữ") : "";
                    sheet.Cells[currentRow, 5].Value = customer.Passport;
                    sheet.Cells[currentRow, 6].Value = customer.NguyenQuan;
                    if (customer.Nationality != null)
                        if (customer.Nationality.Name.ToLower() != "Khong ro")
                            sheet.Cells[currentRow, 7].Value = customer.Nationality.NaNameViet;
                        else
                            sheet.Cells[currentRow, 7].Value = "";
                    currentRow++;
                }
                excelPackage.SaveAs(mem);
            }
            Response.Clear();
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = "";
            if (Cruise != null)
            {
                fileName = string.Format("\"ProvisionalRegister - {0} - {1}.xlsx\"", Date.ToString("dd_MM_yyyy"), Cruise.Name.Replace(" ", "_"));
            }
            Response.AppendHeader("content-disposition", "attachment; filename=" + fileName);
            mem.Position = 0;
            byte[] buffer = mem.ToArray();
            Response.BinaryWrite(buffer);
            Response.End();
        }
        private void ProvisionalRegisterOtherCruise()
        {
            DateTime startDate = Date;
            int cruiseId = -1;
            try
            {
                cruiseId = Convert.ToInt32(Request.QueryString["cruiseid"]);
            }
            catch (Exception) { }
            var bookingStatus = (int)StatusType.Approved;
            var bookings = BookingReportBLL.BookingReportBLL_BookingSearchBy(startDate, cruiseId, bookingStatus);
            var bookings2Days = new List<Booking>();
            var bookings3Days = new List<Booking>();
            foreach (var booking in bookings)
            {
                if (booking.Trip.NumberOfDay == 2)
                    bookings2Days.Add(booking);

                if (booking.Trip.NumberOfDay == 3)
                    bookings3Days.Add(booking);
            }
            var VietNamCustomerOfBookings2Days = new List<Customer>();
            var ForeignCustomerOfBookings2Days = new List<Customer>();
            var VietNamCustomerOfBookings3Days = new List<Customer>();
            var ForeignCustomerOfBookings3Days = new List<Customer>();
            ProvisalRegisterSortCustomer(bookings2Days, ref VietNamCustomerOfBookings2Days, ref ForeignCustomerOfBookings2Days);
            ProvisalRegisterSortCustomer(bookings3Days, ref VietNamCustomerOfBookings3Days, ref ForeignCustomerOfBookings3Days);
            var excelFile = new ExcelFile();
            excelFile.LoadXls(Server.MapPath("/Modules/Sails/Admin/ExportTemplates/DangKyTamTruTemplate.xls"));
            var sheetVietNam2Days = excelFile.Worksheets[0];
            var sheetVietNam3Days = excelFile.Worksheets[1];
            var sheetNuocNgoai2Days = excelFile.Worksheets[2];
            var sheetNuocNgoai3Days = excelFile.Worksheets[3];
            var stt = 1;
            ExportFillProvisalRegister(VietNamCustomerOfBookings2Days, sheetVietNam2Days, ref stt);
            ExportFillProvisalRegister(VietNamCustomerOfBookings3Days, sheetVietNam3Days, ref stt);
            ExportFillProvisalRegister(ForeignCustomerOfBookings2Days, sheetNuocNgoai2Days, ref stt);
            ExportFillProvisalRegister(ForeignCustomerOfBookings3Days, sheetNuocNgoai3Days, ref stt);
            Response.Clear();
            Response.Buffer = true;
            Response.ContentType = "application/vnd.ms-excel";
            var cruise = BookingReportBLL.CruiseGetById(cruiseId);
            if (cruise != null)
                Response.AppendHeader("content-disposition", "attachment; filename=" + string.Format("Form_tam_tru_{0}_{1}.xls", startDate.ToString("dd/MM/yyyy"), cruise.GetModifiedCruiseName().Replace(" ", "_")));
            if (cruise == null)
            {
                return;
            }
            MemoryStream m = new MemoryStream();
            excelFile.SaveXls(m);
            Response.OutputStream.Write(m.GetBuffer(), 0, m.GetBuffer().Length);
            Response.OutputStream.Flush();
            Response.OutputStream.Close();
            m.Close();
            Response.End();
        }
        public void ProvisalRegisterSortCustomer(IList<Booking> bookings, ref List<Customer> vietNamCustomers, ref List<Customer> foreignCustomer)
        {
            foreach (var booking in bookings)
            {
                foreach (var bookingRoom in booking.BookingRooms)
                {
                    foreach (var customer in bookingRoom.Customers)
                    {
                        if (customer.Nationality == null)
                        {
                            foreignCustomer.Add(customer);
                            continue;
                        }

                        if (customer.Nationality.Name == "VIET NAM")
                            vietNamCustomers.Add(customer);
                        else
                            foreignCustomer.Add(customer);
                    }
                }
            }
        }
        public void ExportFillProvisalRegister(IList<Customer> customers, GemBox.Spreadsheet.ExcelWorksheet sheet, ref int stt)
        {
            var activeRow = 1;
            foreach (var customer in customers)
            {
                sheet.Cells[activeRow, 0].Value = stt.ToString();
                stt++;
                customer.Fullname = customer.Fullname ?? "";
                sheet.Cells[activeRow, 1].Value = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(customer.Fullname.ToLower());

                var birthday = "";
                try
                {
                    if (customer.Nationality.Name == "VIET NAM")
                    {
                        birthday = customer.Birthday.Value.ToString("MM/dd/yyyy");
                    }
                    else
                    {
                        birthday = customer.Birthday.Value.ToString("dd/MM/yyyy");
                    }
                }
                catch (Exception) { }
                sheet.Cells[activeRow, 2].Value = birthday;

                sheet.Cells[activeRow, 3].Value = "D";

                var isMale = false;
                try
                {
                    isMale = customer.IsMale.Value;
                }
                catch (Exception) { }

                if (isMale)
                    sheet.Cells[activeRow, 4].Value = "M";
                else
                    sheet.Cells[activeRow, 4].Value = "F";

                var maquoctich = "";
                try
                {
                    maquoctich = customer.Nationality.AbbreviationCode;
                }
                catch (Exception) { }
                sheet.Cells[activeRow, 5].Value = maquoctich;
                sheet.Cells[activeRow, 6].Value = customer.Passport;

                sheet.Cells[activeRow, 7].Value = ((BookingRoom)customer.BookingRooms[0]).Room != null ? ((BookingRoom)customer.BookingRooms[0]).Room.Name : "";
                sheet.Cells[activeRow, 8].Value = ((BookingRoom)customer.BookingRooms[0]).Book.StartDate.ToString("dd/MM/yyyy");
                sheet.Cells[activeRow, 9].Value = ((BookingRoom)customer.BookingRooms[0]).Book.EndDate.ToString("dd/MM/yyyy");
                sheet.Cells[activeRow, 10].Value = ((BookingRoom)customer.BookingRooms[0]).Book.EndDate.ToString("dd/MM/yyyy");
                sheet.Cells[activeRow, 10].Value = customer.NguyenQuan;
                activeRow++;
            }
        }

        protected void btnExport3Day_Click(object sender, EventArgs e)
        {
        }

        protected void btnExportXml_OnClick(object sender, EventArgs e)
        {
            DateTime startDate = Date;
            int cruiseId = -1;
            if (!string.IsNullOrWhiteSpace(Request.QueryString["cruiseid"]))
            {
                cruiseId = Convert.ToInt32(Request.QueryString["cruiseid"]);
            }
            var cruise = Module.GetById<Cruise>(cruiseId);
            var bookingStatus = (int)StatusType.Approved;
            var bookings = BookingReportBLL.BookingReportBLL_BookingSearchBy(startDate, cruiseId, bookingStatus);

            XmlDocument xmlDoc = new XmlDocument();

            XmlNode docNode = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(docNode);

            XmlNode xmlBodyNode = xmlDoc.CreateElement("KHAI_BAO_TAM_TRU");
            var stt = 1;
            foreach (var booking in bookings)
            {
                foreach (var bookingRoom in booking.BookingRooms)
                {
                    foreach (var customer in bookingRoom.Customers)
                    {
                        if (customer.Nationality == null || customer.Nationality.Name != "VIET NAM")
                        {
                            var xmlNode = xmlDoc.CreateElement("THONG_TIN_KHACH");

                            var soThuTu = xmlDoc.CreateElement("so_thu_tu");
                            soThuTu.InnerText = stt.ToString();
                            xmlNode.AppendChild(soThuTu);

                            var hoTen = xmlDoc.CreateElement("ho_ten");
                            hoTen.InnerText = customer.Fullname;
                            xmlNode.AppendChild(hoTen);

                            var ngaySinh = xmlDoc.CreateElement("ngay_sinh");
                            ngaySinh.InnerText = customer.Birthday != null ? customer.Birthday.Value.ToString("dd/MM/yyyy") : "";
                            xmlNode.AppendChild(ngaySinh);

                            var ngaySinhDungDen = xmlDoc.CreateElement("ngay_sinh_dung_den");
                            ngaySinhDungDen.InnerText = "D";
                            xmlNode.AppendChild(ngaySinhDungDen);

                            var gioiTinh = xmlDoc.CreateElement("gioi_tinh");
                            var gt = "";
                            if (customer.IsMale.HasValue)
                            {
                                if (customer.IsMale.Value) gt = "M";
                                else gt = "F";
                            }
                            gioiTinh.InnerText = gt;
                            xmlNode.AppendChild(gioiTinh);

                            var maQuocTich = xmlDoc.CreateElement("ma_quoc_tich");
                            maQuocTich.InnerText = customer.Nationality != null
                                ? customer.Nationality.AbbreviationCode
                                : "";
                            xmlNode.AppendChild(maQuocTich);

                            var soHoChieu = xmlDoc.CreateElement("so_ho_chieu");
                            soHoChieu.InnerText = customer.Passport;
                            xmlNode.AppendChild(soHoChieu);

                            var soPhong = xmlDoc.CreateElement("so_phong");
                            soPhong.InnerText = bookingRoom.Room != null ? bookingRoom.Room.Name : "";
                            xmlNode.AppendChild(soPhong);

                            var ngayDen = xmlDoc.CreateElement("ngay_den");
                            ngayDen.InnerText = booking.StartDate.ToString("dd/MM/yyyy");
                            xmlNode.AppendChild(ngayDen);

                            var ngayDiDuKien = xmlDoc.CreateElement("ngay_di_du_kien");
                            ngayDiDuKien.InnerText = booking.EndDate.ToString("dd/MM/yyyy");
                            xmlNode.AppendChild(ngayDiDuKien);

                            var ngayTraPhong = xmlDoc.CreateElement("ngay_tra_phong");
                            ngayTraPhong.InnerText = booking.EndDate.ToString("dd/MM/yyyy");
                            xmlNode.AppendChild(ngayTraPhong);

                            xmlBodyNode.AppendChild(xmlNode);
                            stt++;
                        }
                    }
                }
            }
            xmlDoc.AppendChild(xmlBodyNode);


            //MemoryStream m = new MemoryStream();
            //xmlDoc.Save(m);
            string attachment = string.Format("attachment; filename=KHAI_BAO_TAM_TRU_{0}_{1}.xml", cruise.Name, startDate.ToString("dd/MM/yyyy"));
            Response.ClearContent();
            Response.ContentType = "application/xml";
            Response.AddHeader("content-disposition", attachment);

            //Response.OutputStream.Write(m.GetBuffer(), 0, m.GetBuffer().Length);
            Response.Write(xmlDoc.InnerXml);
            //Response.OutputStream.Flush();
            //Response.OutputStream.Close();
            //m.Close();
            Response.End();
        }

        //protected void rptBookingList_OnItemDataBound(object sender, RepeaterItemEventArgs e)
        //{
        //    var booking = e.Item.DataItem as Booking;
        //    if (booking != null)
        //    {
        //        var litPickupTime = e.Item.FindControl("litPickupTime") as Literal;
        //        var txtPickupTime = e.Item.FindControl("txtPickupTime") as TextBox;
        //        if (litPickupTime != null && txtPickupTime != null)
        //        {
        //            if (string.IsNullOrWhiteSpace(booking.PickupTime))
        //            {
        //                if (booking.Cruise.Code.Contains("OS") || booking.Cruise.Code.Contains("ST"))
        //                {
        //                    booking.PickupTime = "08:30";
        //                }
        //                else if (booking.Cruise.Code.Contains("NCL"))
        //                {
        //                    booking.PickupTime = "09:30";
        //                }
        //            }
        //            litPickupTime.Text = booking.PickupTime;
        //            txtPickupTime.Text = booking.PickupTime;

        //            if (UserIdentity.IsInRole("Operation") || UserIdentity.HasPermission(AccessLevel.Administrator))
        //            {
        //                litPickupTime.Visible = false;
        //                txtPickupTime.Visible = true;
        //            }
        //            else
        //            {
        //                litPickupTime.Visible = true;
        //                txtPickupTime.Visible = false;
        //            }
        //        }
        //    }
        //}

        //protected void btnSavePickupTime_OnClick(object sender, EventArgs e)
        //{
            //foreach (RepeaterItem item in rptBookingList.Items)
            //{
            //    var hidId = item.FindControl("hidId") as HiddenField;
            //    var txtPickupTime = item.FindControl("txtPickupTime") as TextBox;
            //    if (hidId != null & txtPickupTime != null)
            //    {
            //        if (!string.IsNullOrWhiteSpace(txtPickupTime.Text))
            //        {
            //            var booking = Module.GetById<Booking>(Convert.ToInt32(hidId.Value));
            //            if (booking != null)
            //            {
            //                booking.PickupTime = txtPickupTime.Text;
            //                Module.SaveOrUpdate(booking);
            //            }
            //        }
            //    }
            //}
            //rptBookingList.DataSource = ListBooking.OrderBy(x => x.Trip.Id).ToList();
            //rptBookingList.DataBind();
        //}

        protected void rptTrips_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.DataItem is SailsTrip)
            {
                var trip = (SailsTrip)e.Item.DataItem;
                HyperLink hplTrips = e.Item.FindControl("hplTrips") as HyperLink;
                hplTrips.CssClass = "btn btn-default";
                if (hplTrips != null)
                {
                    if (trip.Id.ToString() == Request.QueryString["tripid"])
                    {
                        hplTrips.CssClass = "btn btn-default active";
                    }

                    var numberOfPax = BookingReportBLL.CustomerGetRowCountByCriterion(trip, Date);
                    hplTrips.Text = string.Format("{0} ({1} pax)", trip.TripCode, numberOfPax.ToString());
                    hplTrips.NavigateUrl = string.Format(
                        "BookingReport.aspx?NodeId={0}&SectionId={1}&Date={2}&cruiseid={3}&tripid={4}", Node.Id, Section.Id,
                        Date.ToString("dd/MM/yyyy"), Cruise.Id, trip.Id);
                }
            }
            else
            {
                HyperLink hplTrips = e.Item.FindControl("hplTrips") as HyperLink;
                if (hplTrips != null)
                {
                    if (Request.QueryString["tripid"] == null)
                    {
                        hplTrips.CssClass = "btn btn-default active";
                    }
                    hplTrips.NavigateUrl = string.Format(
                         "BookingReport.aspx?NodeId={0}&SectionId={1}&Date={2}&cruiseid={3}", Node.Id, Section.Id,
                         Date.ToString("dd/MM/yyyy"), Cruise.Id);
                }
            }
        }
    }
}