using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using NHibernate.Criterion;
using Portal.Modules.OrientalSails.Utils;
using log4net;
using Portal.Modules.OrientalSails.Domain;
using Portal.Modules.OrientalSails.ReportEngine;
using Portal.Modules.OrientalSails.Web.Controls;
using Portal.Modules.OrientalSails.Web.UI;
using Portal.Modules.OrientalSails.Web.Util;
using CMS.Core.Domain;
using System.Web.Services;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Web.Hosting;
using System.Web.UI.HtmlControls;
using Portal.Modules.OrientalSails.BusinessLogic;
using System.Linq;
using Portal.Modules.OrientalSails.Enums;
using System.Drawing;
using Portal.Modules.Orientalsails.Service.Share;
using Portal.Modules.OrientalSails.BusinessLogic.Share;
using Portal.Modules.OrientalSails.Web.Admin.Enums;
using Newtonsoft.Json;

namespace Portal.Modules.OrientalSails.Web.Admin
{
    public partial class BookingView : SailsAdminBasePage
    {

        private readonly ILog _logger = LogManager.GetLogger(typeof(BookingView));
        private string[] arr;
        private int NumberOfDay = 0;

        public BookingViewBLL BookingViewBLL
        {
            get; set;
        }

        public Booking Booking
        {
            get; set;
        }

        public UserBLL UserBLL
        {
            get; set;
        }

        public User CurrentUser
        {
            get; set;
        }

        public PermissionBLL PermissionBLL
        {
            get; set;
        }

        protected IList ExtraServices
        {
            get; set;
        }

        public EmailService EmailService
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

        public bool CanViewTotalDetails
        {
            get; set;
        }

        public bool IsSeatingCruise
        {
            get; set;
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            DataBind();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            BookingViewBLL = new BookingViewBLL();
            try
            {
                if (Request.QueryString["bi"] != null)
                    Booking = BookingViewBLL.BookingGetById(Convert.ToInt32(Request.QueryString["bi"]));
                if (Booking == null)
                {
                    Response.Redirect(string.Format("BookingList.aspx?NodeId={0}&SectionId={1}", Node.Id, Section.Id));
                }
            }
            catch (Exception)
            {
                Response.Redirect(string.Format("BookingList.aspx?NodeId={0}&SectionId={1}", Node.Id, Section.Id));
            }

            UserBLL = new UserBLL();
            PermissionBLL = new PermissionBLL();
            CurrentUser = UserBLL.UserGetCurrent();
            ExtraServices = Module.ExtraOptionGetAll();
            EmailService = new EmailService();
            CanViewSpecialRequestFood = PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.SPECIALREQUEST_FOOD);
            CanViewSpecialRequestRoom = PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.SPECIALREQUEST_ROOM);
            CanViewTotalDetails = PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.EDIT_TOTAL_DETAILS) && PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.EDIT_TOTAL);
            IsSeatingCruise = Booking.Cruise.CruiseType == Enums.CruiseType.Seating;

            var addRoom = string.Format("return addRoom({0})", Booking.Id);
            btnAddRoom.Attributes.Add("onclick", addRoom);
            if (!IsPostBack)
            {
                ControlLoadData();
                BookingLoadData();
                WarningShowIfNotBookingOwner();
                WarningShowIfCruiseLocked();
                rptBusType.DataSource = BookingViewBLL.BusTypeGetAll().List();
                rptBusType.DataBind();
                if (Booking.Transfer_Service == "One Way")
                {
                    rbtTransferService_OneWay.Checked = true;
                }
                else
                    if (Booking.Transfer_Service == "Two Way")
                {
                    rbtTransferService_TwoWay.Checked = true;
                }
                txtTransfer_Note.Text = Booking.Transfer_Note;
                ddlTransferLocation.SelectedValue = ((int)Booking.DIA_DIEM_TRANSFER).ToString();

                rptSalesPriceInput.DataSource = Booking.RoomPriceSalesInputs;
                rptSalesPriceInput.DataBind();

                var seatingCruiseRoomPrice = Booking.BookingRoomPrices.FirstOrDefault(x => x.RoomClass == null && x.RoomType == null);
                if (seatingCruiseRoomPrice != null)
                {
                    txtNumberOfAdultsPrice.Attributes.Add("ng-init", "txtNumberOfAdultsPrice=" + seatingCruiseRoomPrice.PriceOfAdult.ToString());
                    txtNumberOfChildsPrice.Attributes.Add("ng-init", "txtNumberOfChildsPrice=" + seatingCruiseRoomPrice.PriceOfChild.ToString());
                    txtNumberOfBabysPrice.Attributes.Add("ng-init", "txtNumberOfBabysPrice=" + seatingCruiseRoomPrice.PriceOfBaby.ToString());
                }
                else
                {
                    txtNumberOfAdultsPrice.Attributes.Add("ng-init", "txtNumberOfAdultsPrice=0");
                    txtNumberOfChildsPrice.Attributes.Add("ng-init", "txtNumberOfChildsPrice=0");
                    txtNumberOfBabysPrice.Attributes.Add("ng-init", "txtNumberOfBabysPrice=0");
                }
                BookingHistorySave();
            }

            if (Booking.Cruise.CruiseType == Enums.CruiseType.Cabin)
            {
                plhNumberOfCabin.Visible = true;
                plhCruiseCabinControlPanel.Visible = true;
                plhCruiseSeatingControlPanel.Visible = false;
                plhCruiseCabinCustomer.Visible = true;
                plhCruiseSeatingCustomer.Visible = false;
            }
            else if (Booking.Cruise.CruiseType == Enums.CruiseType.Seating)
            {
                plhNumberOfCabin.Visible = false;
                plhCruiseCabinControlPanel.Visible = false;
                plhCruiseSeatingControlPanel.Visible = true;
                plhCruiseSeatingCustomer.Visible = true;
                plhCruiseCabinCustomer.Visible = false;
            }

            bookingViewController.Attributes.Add("ng-init", "bookingId=" + Booking.Id + ";roomPriceSalesInputs=" + JsonConvert.SerializeObject(Booking.RoomPriceSalesInputs) + ";isSeatingCruise=" + IsSeatingCruise.ToString().ToLower() + ";canViewTotalDetails=" + CanViewTotalDetails.ToString().ToLower());
            txtTotal.Attributes.Add("ng-init", "actuallyCollected=" + Booking.Total);

        }

        public void WarningShowIfCruiseLocked()
        {
            var isLocked = false;
            var cruiseId = -1;
            try
            {
                cruiseId = Booking.Cruise.Id;
            }
            catch { }

            DateTime? startDate = Booking.StartDate;
            DateTime? endDate = Booking.EndDate;
            var locks = BookingViewBLL.LockedGetBy(startDate, endDate, cruiseId);
            if (locks.Count() > 0)
                isLocked = true;

            string lockDate = "";
            foreach (var locked in locks)
            {
                lockDate = lockDate + locked.Start.ToString("dd/MM/yyyy") + ",";
            }
            if (lockDate.Length > 0)
                lockDate = lockDate.Remove(lockDate.Length - 1);

            if (isLocked)
            {
                try
                {
                    ShowWarning("Cruise " + Booking.Cruise.Name + " is locked on " + lockDate);
                }
                catch { }
            }
        }

        public void WarningShowIfNotBookingOwner()
        {
            try
            {
                var warning = "You're editing booking someone else is in charge, please noticed that if you submit any changes an email will be send to him/her";

                if (Booking.Agency != null && Booking.Agency.Sale != null && CurrentUser.Id != Booking.Agency.Sale.Id)
                    ShowWarning(warning);
            }
            catch { }
        }

        protected void Page_Unload(object sender, EventArgs e)
        {
            if (BookingViewBLL != null)
            {
                BookingViewBLL.Dispose();
                BookingViewBLL = null;
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

        protected void rptRoomList_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            CustomersInfo.rptRoomList_itemDataBound(sender, e, Module, false, null, this, ddlRoomTypes.Items);
        }

        protected void rptAdults_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            var customerInfo1 = (CustomerInfoRowInput)e.Item.FindControl("customer1");
            customerInfo1.GetCustomer((Customer)e.Item.DataItem, Module);
        }

        protected void rptBabies_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            var customerInfo1 = (CustomerInfoRowInput)e.Item.FindControl("customer1");
            customerInfo1.GetCustomer((Customer)e.Item.DataItem, Module);
        }

        protected void rptChildren_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            var customerInfo1 = (CustomerInfoRowInput)e.Item.FindControl("customer1");
            customerInfo1.GetCustomer((Customer)e.Item.DataItem, Module);
        }

        protected void buttonSubmit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Request.Params[txtPickup.UniqueID]))
            {
                ShowWarning("Pick up address is required.");
            }

            //ScreenCaptureSave();
            BookingSetData();
            BookingViewBLL.BookingSaveOrUpdate(Booking);
            ShowSuccess("Booking updated. Please fix errors if exists and submit again");

            bool needEmail = !Booking.Special && chkSpecial.Checked;
            string email = string.Empty;
            if (needEmail)
            {
                email = "&confirm=1";
            }
            foreach (RepeaterItem extraService in rptExtraServices.Items)
            {
                var chkService = (HtmlInputCheckBox)extraService.FindControl("chkService");
                if (chkService.Checked)
                {
                    SaveExtraService();
                }
                else
                {
                    DeleteExtraService();
                }
            }
            Session["Redirect"] = true;
            PageRedirect(string.Format("BookingView.aspx?NodeId={0}&SectionId={1}&bi={2}{3}", Node.Id, Section.Id, Booking.Id, email));
        }

        public void BookingSetData()
        {
            Booking.Inspection = chkInspection.Checked;
            Booking.CancelledReason = txtCancelledReason.Text;
            Booking.AgencyCode = txtAgencyCode.Text;
            Booking.PickupAddress = txtPickup.Text;
            Booking.PickupTime = txtPickupTime.Text;
            Booking.SpecialRequest = txtSpecialRequest.Text;
            Booking.SpecialRequestRoom = txtSpecialRequestRoom.Text;
            Booking.IsPaymentNeeded = chkIsPaymentNeeded.Checked;
            Booking.Note = txtCustomerInfo.Text;
            Booking.Special = chkSpecial.Checked;
            Booking.IsCharter = chkCharter.Checked;
            Booking.IsEarlyBird = chkEarlyBird.Checked;
            Booking.CO_AN_CHAY = chkAnChay.Checked;

            try
            {
                Booking.Agency = Module.AgencyGetById(Convert.ToInt32(ddlAgencies.SelectedValue));
            }
            catch { }

            if (Booking.Agency == null)
            {
                ShowErrors("Please select one agency");
            }

            try
            {
                Booking.Deadline = DateTime.ParseExact(txtDeadline.Text, "dd/MM/yyyy HH:mm",
                                                        CultureInfo.InvariantCulture);
            }
            catch { }

            try
            {
                Booking.Booker = Module.ContactGetById(Convert.ToInt32(cddlBooker.SelectedValue));
            }
            catch { }

            try
            {
                Booking.IsTotalUsd = Convert.ToBoolean(Int32.Parse(ddlCurrencies.SelectedValue));
            }
            catch { }

            try
            {
                Booking.CancelPay = Convert.ToDouble(txtPenalty.Text);
            }
            catch { }

            BookingStatusProcess();
            TripProcess();
            CruiseProcess();
            StartDateProcess();
            CharterProcess();//not cleanly
            ExtraServicesProcess(); //not cleanly
            VoucherProcess();//not cleany
            TotalPriceProcess();//not cleany
            BookingRoomProcess();
            CruiseSeatingCustomerProcess();
            RoomSalesPriceInputProcess();


            if (Booking.Trip.NumberOfDay == 3)
            {
                Booking.TripOption = (TripOption)ddlOptions.SelectedIndex;
            }

            var seriesCode = "";
            seriesCode = txtSeriesCode.Text;

            if (!String.IsNullOrEmpty(seriesCode))
            {
                var series = BookingViewBLL.SeriesGetBySeriesCode(seriesCode);
                if (series != null)
                    Booking.Series = series;
                else
                    ShowErrors("Không tồn tại series này");
            }
        }

        public void RoomSalesPriceInputProcess()
        {
            if (IsSeatingCruise)
            {
                double adultPrice = 0;
                double childPrice = 0;
                double babyPrice = 0;
                Double.TryParse(txtNumberOfAdultsPrice.Value, out adultPrice);
                Double.TryParse(txtNumberOfChildsPrice.Value, out childPrice);
                Double.TryParse(txtNumberOfBabysPrice.Value, out babyPrice);

                var bkrp = new BookingRoomPrice()
                {
                    Booking = Booking,
                    PriceOfAdult = adultPrice,
                    PriceOfChild = childPrice,
                    PriceOfBaby = babyPrice,
                };

                if (Booking.BookingRoomPrices != null && Booking.BookingRoomPrices.Count > 0)
                {
                    bkrp = Booking.BookingRoomPrices.First(x => x.RoomClass == null && x.RoomType == null);
                    bkrp.PriceOfAdult = adultPrice;
                    bkrp.PriceOfChild = childPrice;
                    bkrp.PriceOfBaby = babyPrice;
                }

                Module.SaveOrUpdate(bkrp);
            }

            foreach (RepeaterItem item in rptSalesPriceInput.Items)
            {
                var txtNumberOfRoomsPrice = (HtmlInputText)item.FindControl("txtNumberOfRoomsPrice");
                var txtNumberOfAddAdultPrice = (HtmlInputText)item.FindControl("txtNumberOfAddAdultPrice");
                var txtNumberOfAddChildPrice = (HtmlInputText)item.FindControl("txtNumberOfAddChildPrice");
                var txtNumberOfAddBabyPrice = (HtmlInputText)item.FindControl("txtNumberOfAddBabyPrice");
                var txtNumberOfExtrabedPrice = (HtmlInputText)item.FindControl("txtNumberOfExtrabedPrice");
                var roomClassId = (HiddenField)item.FindControl("hiddenRoomClassId");
                var roomTypeId = (HiddenField)item.FindControl("hiddenRoomTypeId");
                double roomPrice = 0;
                double addAdultPrice = 0;
                double addChildPrice = 0;
                double addBabyPrice = 0;
                double extrabedPrice = 0;
                Double.TryParse(txtNumberOfRoomsPrice.Value, out roomPrice);
                Double.TryParse(txtNumberOfAddAdultPrice.Value, out addAdultPrice);
                Double.TryParse(txtNumberOfAddChildPrice.Value, out addChildPrice);
                Double.TryParse(txtNumberOfAddBabyPrice.Value, out addBabyPrice);
                Double.TryParse(txtNumberOfExtrabedPrice.Value, out extrabedPrice);
                RoomClass rclass = Module.RoomClassGetById(Convert.ToInt32(roomClassId.Value));
                RoomTypex rtype = Module.RoomTypexGetById(Convert.ToInt32(roomTypeId.Value));

                var bkrp = new BookingRoomPrice()
                {
                    Booking = Booking,
                    PriceOfRoom = roomPrice,
                    PriceOfAddAdult = addAdultPrice,
                    PriceOfAddChild = addChildPrice,
                    PriceOfAddBaby = addBabyPrice,
                    PriceOfExtrabed = extrabedPrice,
                    RoomClass = rclass,
                    RoomType = rtype,
                };

                if (Booking.BookingRoomPrices != null && Booking.BookingRoomPrices.Count > 0)
                {
                    bkrp = Booking.BookingRoomPrices.FirstOrDefault(x => x.RoomClass.Id == rclass.Id && x.RoomType.Id == rtype.Id);
                    if (bkrp != null)
                    {
                        bkrp.PriceOfRoom = roomPrice;
                        bkrp.PriceOfAddAdult = addAdultPrice;
                        bkrp.PriceOfAddChild = addChildPrice;
                        bkrp.PriceOfAddBaby = addBabyPrice;
                        bkrp.PriceOfExtrabed = extrabedPrice;
                        Module.SaveOrUpdate(bkrp);
                    }
                }
            }
        }

        public void CruiseSeatingCustomerProcess()
        {
            foreach (RepeaterItem item in rptAdults.Items)
            {
                var customerInfo1 = (CustomerInfoRowInput)item.FindControl("customer1");
                Customer customer1 = customerInfo1.NewCustomer(Module);
                Module.SaveOrUpdate(customer1);
            }
            foreach (RepeaterItem item in rptChildren.Items)
            {
                var customerInfo1 = (CustomerInfoRowInput)item.FindControl("customer1");
                Customer customer1 = customerInfo1.NewCustomer(Module);
                Module.SaveOrUpdate(customer1);
            }
            foreach (RepeaterItem item in rptBabies.Items)
            {
                var customerInfo1 = (CustomerInfoRowInput)item.FindControl("customer1");
                Customer customer1 = customerInfo1.NewCustomer(Module);
                Module.SaveOrUpdate(customer1);
            }
        }

        public void BookingRoomProcess()
        {
            bool canChange = false;
            foreach (RepeaterItem item in rptRoomList.Items)
            {
                var txtRoomNumber = (TextBox)item.FindControl("txtRoomNumber");
                var hiddenBookingRoomId = (HiddenField)item.FindControl("hiddenBookingRoomId");
                var checkboxAddExtraBed = (HtmlInputCheckBox)item.FindControl("checkboxAddExtraBed");
                var checkBoxAddAdult = (HtmlInputCheckBox)item.FindControl("checkBoxAddAdult");
                var checkBoxAddChild = (HtmlInputCheckBox)item.FindControl("checkBoxAddChild");
                var checkBoxAddBaby = (HtmlInputCheckBox)item.FindControl("checkBoxAddBaby");
                var checkBoxSingle = (HtmlInputCheckBox)item.FindControl("checkBoxSingle");
                var customerInfo1 = (CustomerInfoRowInput)item.FindControl("customer1");
                var customerInfo2 = (CustomerInfoRowInput)item.FindControl("customer2");

                BookingRoom bookingRoom = Module.BookingRoomGetById(Convert.ToInt32(hiddenBookingRoomId.Value));
                bookingRoom.HasBaby = checkBoxAddBaby.Checked;
                bookingRoom.HasChild = checkBoxAddChild.Checked;
                bookingRoom.IsSingle = checkBoxSingle.Checked;
                bookingRoom.RoomNumber = txtRoomNumber.Text;
                bookingRoom.HasAddExtraBed = checkboxAddExtraBed.Checked;
                bookingRoom.HasAddAdult = checkBoxAddAdult.Checked;


                DropDownList ddlRoomTypeChanger = (DropDownList)item.FindControl("ddlRoomTypes");
                var stringBookingRoomTypeClass = bookingRoom.RoomType.Id.ToString();
                if (ddlRoomTypeChanger.Visible && !string.IsNullOrWhiteSpace(ddlRoomTypeChanger.SelectedValue) && ddlRoomTypeChanger.SelectedValue != stringBookingRoomTypeClass)
                {

                    RoomTypex newType = Module.RoomTypexGetById(Convert.ToInt32(ddlRoomTypeChanger.SelectedValue));
                    bookingRoom.RoomType = newType;

                }
                Module.SaveOrUpdate(bookingRoom);
                bookingRoom.Customers.Clear();

                Customer customer1 = customerInfo1.NewCustomer(Module);
                customer1.Booking = Booking;
                customer1.BookingRooms.Clear();
                customer1.BookingRooms.Add(bookingRoom);
                customer1.Type = CustomerType.Adult;
                Module.SaveOrUpdate(customer1);

                if (!checkBoxSingle.Checked)
                {
                    Customer customer2 = customerInfo2.NewCustomer(Module);
                    customer2.Booking = Booking;
                    customer2.BookingRooms.Clear();
                    customer2.BookingRooms.Add(bookingRoom);
                    customer2.Type = CustomerType.Adult;
                    Module.SaveOrUpdate(customer2);

                }

                CustomerInfoRowInput customerChild = (CustomerInfoRowInput)item.FindControl("customerChild");
                Customer child = customerChild.NewCustomer(Module);
                if (checkBoxAddChild.Checked)
                {
                    child.Booking = Booking;
                    child.BookingRooms.Clear();
                    child.BookingRooms.Add(bookingRoom);
                    child.Type = CustomerType.Children;
                    Module.SaveOrUpdate(child);
                }

                CustomerInfoRowInput customerBaby = (CustomerInfoRowInput)item.FindControl("customerBaby");
                Customer baby = customerBaby.NewCustomer(Module);
                if (checkBoxAddBaby.Checked)
                {
                    baby.Booking = Booking;
                    baby.BookingRooms.Clear();
                    baby.BookingRooms.Add(bookingRoom);
                    baby.Type = CustomerType.Baby;
                    Module.SaveOrUpdate(baby);
                }
                CustomerExtraInfoRowInput customerExtra = (CustomerExtraInfoRowInput)item.FindControl("customerExtraBed");
                Customer extra = customerExtra.NewCustomer(Module);
                if (checkBoxAddAdult.Checked)
                {
                    extra.Booking = Booking;
                    extra.BookingRooms.Clear();
                    extra.BookingRooms.Add(bookingRoom);
                    extra.HasAddAdult = true;
                    if (customerExtra.IsBaby)
                        extra.Type = CustomerType.Baby;
                    else if (customerExtra.IsChild)
                        extra.Type = CustomerType.Children;
                    else extra.Type = CustomerType.Adult;
                    Module.SaveOrUpdate(extra);
                }
            }

        }

        public void TotalPriceProcess()
        {
            if (!txtTotal.ReadOnly)
            {
                double total = Convert.ToDouble(txtTotal.Text);
                double finalTotal = total;

                if (total <= 0)
                {
                    try
                    {
                        finalTotal = Booking.Calculate(Module, null, ChildPrice,
                                                        Convert.ToDouble(Module.ModuleSettings("AgencySupplement")),
                                                        CustomPriceForRoom, true);
                    }
                    catch (Exception ex)
                    {
                        ShowErrors(Resources.errorCanNotCalculatePrice);
                    }
                }
                else
                {
                    finalTotal = total;
                }

                if (Booking.Total != finalTotal)
                {
                    Booking.AccountingStatus = AccountingStatus.Modified;
                    Booking.Total = finalTotal;
                }
            }
        }

        public void VoucherProcess()
        {
            var errorMessage = "";
            var voucherAdded = "";
            if (txtAllVoucher.Text.ToLower() != "ov")
            {
                var trimmedCode = txtAllVoucher.Text.Trim();
                if (trimmedCode.EndsWith(";"))
                    arr = trimmedCode.Remove(trimmedCode.Length - 1).Split(new char[] { ';' });
                else
                    arr = trimmedCode.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);


                foreach (string codeString in arr)
                {

                    var code = 0u;
                    try
                    {
                        code = Convert.ToUInt32(codeString.Trim());
                    }
                    catch
                    {
                        errorMessage += string.Format("Voucher code {0} invalid!", codeString) + "<br/>";
                    }

                    int batchid;
                    int index;
                    VoucherCodeEncryption.Decrypt(code, out batchid);

                    ICriterion critCheckVoucherAlreadyInBooking = Expression.Eq("Code", code.ToString());
                    critCheckVoucherAlreadyInBooking = Expression.Eq("Booking.Id", Booking.Id);
                    var voucherAlreadyInBooking = Module.GetObject<BookingVoucher>(critCheckVoucherAlreadyInBooking, 0, 0);
                    if (voucherAlreadyInBooking.Count > 0)
                    {
                        //Nếu đã có voucher trong booking thì không cần làm gì nữa
                        voucherAdded += codeString + ";";
                        continue;
                    }

                    //Nếu chưa có voucher trong booking thì thêm vào
                    ICriterion crit = Expression.Eq("Code", code.ToString());
                    crit = Expression.And(crit, Expression.Not(Expression.Eq("Booking.Id", Booking.Id)));
                    bool isUsed = false;
                    foreach (BookingVoucher bv in Module.GetObject<BookingVoucher>(crit, 0, 0))
                    {
                        if (bv.Booking.Status == StatusType.Approved || bv.Booking.Status == StatusType.Pending)
                            isUsed = true;
                    }

                    if (isUsed)
                    {
                        errorMessage += string.Format("Voucher code {0} already used!", codeString) + "<br/>";
                        continue;
                    }

                    var batch = Module.GetObject<VoucherBatch>(batchid);

                    if (batch == null)
                    {
                        errorMessage += string.Format("Voucher code {0} invalid!", codeString) + "<br/>";
                        continue;
                    }
                    else if (batch.ValidUntil < Booking.StartDate)
                    {
                        errorMessage += string.Format("Voucher code {0} outdated!", codeString) + "<br/>";
                        continue;
                    }

                    Booking.Batch = batch;

                    var bkv = Module.GetBookingVoucher(Booking, codeString);
                    bkv.Voucher = batch;

                    Module.SaveOrUpdate(bkv, UserIdentity);
                    voucherAdded += codeString + ";";
                    //
                }

                //Nếu voucher mới nhập bị thừa so với lần nhập trước thì xóa số voucher thừa đi
                //Nếu đã clear hết voucher đi thì xóa hết số voucher của lần nhập trước trong booking

                ICriterion critVoucherInBooking = Expression.Eq("Booking.Id", Booking.Id);
                var allVoucherAlreadyInBooking = Module.GetObject<BookingVoucher>(critVoucherInBooking, 0, 0);
                if (arr.Length == 0)
                {
                    foreach (BookingVoucher bv in allVoucherAlreadyInBooking)
                    {
                        Module.Delete(bv);
                    }
                }
                else
                {
                    var voucherNeedDelete = allVoucherAlreadyInBooking.Where(x => !arr.Contains(x.Code));
                    foreach (BookingVoucher bv in voucherNeedDelete)
                    {
                        Module.Delete(bv);
                    }
                }
            }

            Booking.VoucherCode = voucherAdded;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                ShowErrors(errorMessage);
            }
        }

        public void ExtraServicesProcess()
        {
            foreach (RepeaterItem item in rptExtraServices.Items)
            {
                HiddenField hiddenId = (HiddenField)item.FindControl("hiddenId");
                HtmlInputCheckBox chkService = (HtmlInputCheckBox)item.FindControl("chkService");
                ExtraOption option = Module.ExtraOptionGetById(Convert.ToInt32(hiddenId.Value));
                BookingExtra extra = Module.BookingExtraGet(Booking, option);
                if (extra.Id <= 0 && chkService.Checked)
                {
                    Module.SaveOrUpdate(extra);
                }
                if (extra.Id > 0 && !chkService.Checked)
                {
                    Module.Delete(extra);
                }
            }
        }

        public void CharterProcess()
        {
            Locked charter = null;
            if (Booking.Charter != null)
            {
                charter = Booking.Charter;
                if (!chkCharter.Checked || Booking.Status != StatusType.Approved)
                {
                    Module.Delete(charter);
                    charter = null;
                }
            }
            else
            {
                if (chkCharter.Checked && Booking.Status == StatusType.Approved)
                {
                    charter = new Locked();
                    charter.Charter = Booking;
                    charter.Cruise = Booking.Cruise;
                    charter.Description = "Booking charter";
                }
            }

            if (charter != null)
            {
                charter.Start = Booking.StartDate;
                charter.End = Booking.EndDate.AddDays(-1);
                Module.SaveOrUpdate(charter);
            }
            Booking.Charter = charter;
        }

        public void CruiseProcess()
        {
            bool haveRoomAvaiable = RoomCheckAvaiable();
            var cruise = GetCruise();
            var trip = GetTrip();
            bool didChangeCruise = false;
            try
            {
                didChangeCruise = cruise.Id != Booking.Cruise.Id ? true : false;
            }
            catch { }

            bool doCruiseRunTrip = CruiseRunTripCheck();

            if (didChangeCruise && !doCruiseRunTrip)
            {
                try
                {
                    ShowErrors(string.Format("Can not change cruise because {0} doesn't run {1} trip", cruise.Name, trip.Name));
                }
                catch { }
                return;
            }

            if (didChangeCruise && !haveRoomAvaiable)
            {
                ShowErrors("Can not change cruise because not enough room on that day. Please check again");
                return;
            }
            Booking.Cruise = cruise;
        }

        public void TripProcess()
        {
            bool haveRoomAvaiable = RoomCheckAvaiable();
            var trip = GetTrip();
            var cruise = GetCruise();
            bool didChangeTrip = false;
            try
            {
                didChangeTrip = trip.Id != Booking.Trip.Id ? true : false;
            }
            catch { }

            bool doCruiseRunTrip = CruiseRunTripCheck();

            if (didChangeTrip && !doCruiseRunTrip)
            {
                try
                {
                    ShowErrors(string.Format("Can not change trip because {0} doesn't run {1} trip", cruise.Name, trip.Name));
                }
                catch { }
                return;
            }

            if (didChangeTrip && !haveRoomAvaiable)
            {
                ShowErrors("Can not change trip because not enough room on that day. Please check again");
                return;
            }

            Booking.Trip = trip;
        }

        public bool CruiseRunTripCheck()
        {
            var trip = GetTrip();
            var cruise = GetCruise();

            try
            {
                if (!cruise.Trips.Select(x => x.Id).Contains(trip.Id))
                {
                    return false;
                }
            }
            catch { return false; }

            return true;
        }

        public void StartDateProcess()
        {
            bool haveRoomAvaiable = RoomCheckAvaiable();
            var startDate = GetStartDate();

            if (haveRoomAvaiable)
            {
                Booking.StartDate = startDate.Value;
                try
                {
                    Booking.EndDate = startDate.Value.AddDays(Booking.Trip.NumberOfDay - 1);
                }
                catch { }
            }

            bool didChangeStartdate = Booking.StartDate != startDate ? true : false;
            if (!haveRoomAvaiable && didChangeStartdate)
                ShowErrors("Can not change start date because not enough room on that day. Please check again");

        }

        public bool RoomCheckAvaiable()
        {
            bool haveRoomAvaiable = false;

            var startDate = GetStartDate();
            var cruise = GetCruise();
            var trip = GetTrip();

            var roomMap = new Dictionary<string, int>();
            var available = new Dictionary<string, int>();
            foreach (BookingRoom room in Booking.BookingRooms)
            {
                string key = room.RoomType.Id + "#" + room.RoomClass.Id;

                if (roomMap.ContainsKey(key))
                    roomMap[key] += 1;
                else
                {
                    roomMap.Add(key, 1);
                    available.Add(key,
                                  Module.RoomCount(room.RoomClass, room.RoomType, cruise, startDate.Value,
                                                   trip.NumberOfDay,
                                                   Booking));
                }
            }

            if (Booking.Status != StatusType.Cancelled)
                haveRoomAvaiable = true;

            foreach (KeyValuePair<string, int> pair in roomMap)
            {
                if (pair.Value > available[pair.Key])
                {
                    haveRoomAvaiable = false;
                    break;
                }
                else
                {
                    haveRoomAvaiable = true;
                }
            }

            return haveRoomAvaiable;
        }

        public void BookingStatusProcess()
        {
            var statusType = (StatusType)Enum.Parse(typeof(StatusType), ddlStatusType.SelectedValue);
            if (statusType == StatusType.Approved)
                Booking.Status = statusType;
            if (statusType == StatusType.Cancelled)
                CancelledStatusProcess();
            if (statusType == StatusType.Pending)
                PendingStatusProcess();
            if (statusType == StatusType.CutOff)
                CutOffStatusProcess();
        }

        public void CutOffStatusProcess()
        {
            var statusType = (StatusType)Enum.Parse(typeof(StatusType), ddlStatusType.SelectedValue);
            try
            {
                Booking.CutOffDays = Int32.Parse(txtCutOffDays.Text.Trim());
            }
            catch
            {
                ShowErrors("CutOffDays is not valid");
                return;
            }
            Booking.Status = statusType;
        }

        private void PendingStatusProcess()
        {
            var statusType = (StatusType)Enum.Parse(typeof(StatusType), ddlStatusType.SelectedValue);
            var bookingHistories = BookingViewBLL.BookingHistoryGetByBookingId(Booking.Id).OrderBy(x => x.Date);
            var bookingLastStatus = bookingHistories.Last().Status;
            var canApplyStatus = bookingLastStatus == StatusType.Approved ? false : true;

            if (canApplyStatus)
                Booking.Status = statusType;
            else
                ShowErrors("Approved booking can not switch to pending");
        }

        public void CancelledStatusProcess()
        {
            bool canSendEmail = false;
            var statusType = (StatusType)Enum.Parse(typeof(StatusType), ddlStatusType.SelectedValue);
            var isEmptyReason = String.IsNullOrEmpty(txtCancelledReason.Text);
            var canApplyCancelled = statusType == StatusType.Cancelled && !isEmptyReason;

            if (canApplyCancelled)
            {
                Booking.Status = statusType;
                canSendEmail = true;
            }
            else
            {
                ShowErrors("Chưa nhập lý do hủy booking. Không thể hủy booking");
                canSendEmail = false;
            }

            if (canSendEmail)
                SendEmailCancelled();
        }

        protected void rptRoomList_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            switch (e.CommandName.ToLower())
            {
                case "delete":
                    BookingRoom bookingRoom = BookingViewBLL.BookingRoomGetById(Convert.ToInt32(e.CommandArgument));
                    Booking.BookingRooms.Remove(bookingRoom);
                    BookingViewBLL.BookingRoomDelete(bookingRoom);
                    break;

            }

            BookingHistorySave();
            Response.Redirect(Request.RawUrl);
        }

        public void BookingTrackSaveAddRoom()
        {
            BookingTrack track = new BookingTrack()
            {
                Booking = Booking,
                ModifiedDate = DateTime.Now,
                User = CurrentUser,
            };

            BookingChanged change = new BookingChanged()
            {
                Action = BookingAction.AddRoom,
                Parameter = ddlRoomTypes.SelectedItem.Text,
                Track = track,
            };
            Module.SaveOrUpdate(change);

            BookingChanged customerChange = new BookingChanged()
            {
                Action = BookingAction.AddCustomer,
                Parameter = string.Format("{0}|{1}|{2}", 2, 0, 0),
                Track = track,
            };
            Module.SaveOrUpdate(customerChange);
        }

        public void BookingRoomAdd(RoomClass roomClass, RoomTypex roomType)
        {
            BookingRoom bookingRoom = new BookingRoom()
            {
                RoomClass = roomClass,
                RoomType = roomType,
                Book = Booking
            };

            Customer customer1 = new Customer()
            {
                Type = CustomerType.Adult,
                BookingRooms = new List<BookingRoom>(),
            };
            customer1.BookingRooms.Add(bookingRoom);

            Customer customer2 = new Customer()
            {
                Type = CustomerType.Adult,
                BookingRooms = new List<BookingRoom>(),
            };
            customer2.BookingRooms.Add(bookingRoom);

            bookingRoom.Customers.Add(customer1);
            bookingRoom.Customers.Add(customer2);

            BookingViewBLL.CustomerSaveOrUpdate(customer1);
            BookingViewBLL.CustomerSaveOrUpdate(customer2);
            BookingViewBLL.BookingRoomSaveOrUpdate(bookingRoom);
        }

        protected void lbtCalculate_Click(object sender, EventArgs e)
        {

        }

        protected void rptExtraServices_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            HtmlInputCheckBox chkService = (HtmlInputCheckBox)e.Item.FindControl("chkService");
            ExtraOption option = (ExtraOption)e.Item.DataItem;
            chkService.Checked = Booking.ExtraServices.Select(x => x.Id).Contains(option.Id);
            chkService.Name = option.Name;
            string script = "var " + option.Name + "=" + chkService.Checked.ToString().ToLower();
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "initChkServiceValue", script, true);
        }

        protected void btnLockIncome_Click(object sender, EventArgs e)
        {
            if (!PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.LOCK_INCOME))
            {
                ShowErrors("You don't have permission to perform this action");
                return;
            }
            Booking.LockDate = DateTime.Now;
            Booking.LockBy = CurrentUser;
            BookingViewBLL.BookingSaveOrUpdate(Booking);
            Response.Redirect(Request.RawUrl);
        }

        protected void btnUnlockIncome_Click(object sender, EventArgs e)
        {
            if (!PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.LOCK_INCOME))
            {
                ShowErrors("You don't have permission to perform this action");
                return;
            }
            Booking.LockDate = new DateTime(1992, 06, 17);
            Booking.LockBy = null;
            BookingViewBLL.BookingSaveOrUpdate(Booking);
            Response.Redirect(Request.RawUrl);
        }

        protected void chkCharter_OnCheckedChanged(object sender, EventArgs e)
        {
            if (chkCharter.Checked)
            {
                DateTime dateTime = DateTime.ParseExact(txtStartDate.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                int cruiseId = Convert.ToInt32(ddlCruises.SelectedValue);
                ICriterion cruiseCriterion = Expression.Eq("Cruise.Id", cruiseId);
                ICriterion compareWithStartDate = Expression.Le("Start", dateTime);
                ICriterion compareWithEndDate = Expression.Ge("End", dateTime);
                ICriterion dateCriterion = Expression.And(compareWithStartDate, compareWithEndDate);
                ICriterion finalCriterion = Expression.And(cruiseCriterion, dateCriterion);
                var cruiseBeLocked = Module.GetObject<Locked>(finalCriterion, 0, 0);
                if (cruiseBeLocked != null)
                {
                    if (cruiseBeLocked.Count > 0)
                    {
                        ShowErrors("Tàu này đã có booking charter trước không được phép charter");
                        chkCharter.Checked = false;
                    }

                }
                else
                {
                    throw new Exception("cruiseBelock variable is null");
                }

                compareWithStartDate = Expression.Le("StartDate", dateTime);
                compareWithEndDate = Expression.Ge("EndDate", dateTime);
                dateCriterion = Expression.And(compareWithStartDate, compareWithEndDate);
                finalCriterion = Expression.And(cruiseCriterion, dateCriterion);
                var bookingListOnDay = Module.GetObject<Booking>(finalCriterion, 0, 0);

                if (bookingListOnDay != null)
                {
                    if (bookingListOnDay.Count > 0)
                    {
                        ShowErrors("Tàu này đã có booking không được phép charter");
                        chkCharter.Checked = false;
                    }
                }
                else
                {
                    throw new Exception("bookingListOnDay = null");
                }
            }
        }

        public void ControlLoadData()
        {
            ddlStatusType.DataSource = Enum.GetNames(typeof(StatusType));
            ddlStatusType.DataBind();

            ddlAgencies.DataSource = BookingViewBLL.AgencyGetAll().OrderBy(x => x.Name);
            ddlAgencies.DataTextField = "Name";
            ddlAgencies.DataValueField = "Id";
            ddlAgencies.DataBind();
            ddlAgencies.Items.Insert(0, "-- Agency --");

            var trips = BookingViewBLL.TripGetAll();
            foreach (var trip in trips)
            {
                var listItemTrip = new ListItem(trip.Name, trip.Id.ToString());
                if (trip.NumberOfOptions == 2)
                {
                    listItemTrip.Attributes.Add("data-option-visible", "true");
                }
                ddlTrips.Items.Add(listItemTrip);
            }

            ddlCruises.DataSource = BookingViewBLL.CruiseGetAll();
            ddlCruises.DataTextField = "Name";
            ddlCruises.DataValueField = "Id";
            ddlCruises.DataBind();
            if (ddlCruises.Items.Count == 1)
            {
                ddlCruises.Visible = false;
            }

            cddlBooker.Items.Insert(0, "-- Booker --");
            cddlBooker.DataSource = Module.ContactGetAllEnabled();
            cddlBooker.DataTextField = "Name";
            cddlBooker.DataValueField = "Id";
            cddlBooker.DataParentField = "AgencyId";
            cddlBooker.ParentClientID = ddlAgencies.ClientID;
            cddlBooker.DataBind();



            rptExtraServices.DataSource = Module.ExtraOptionGetBooking();
            rptExtraServices.DataBind();

            TotalDisplay();
            TotalLockedDisplay();
            AddRoomControlGenerate();
            CustomerBirthdayDisplay();

        }

        public void TotalDisplay()
        {
            var canViewTotal = PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.VIEW_TOTAL_BY_DATE);
            if (!canViewTotal)
                HideTotal();

        }

        public void HideTotal()
        {
            txtTotal.Visible = false;
            ddlCurrencies.Visible = false;
        }

        public void BookingLoadData()
        {
            lblBookingId.Text = string.Format("OS{0:00000}", Booking.Id);
            txtTotal.Text = Booking.Total.ToString("#,0.#");
            ddlStatusType.SelectedValue = Enum.GetName(typeof(StatusType), Booking.Status);
            txtStartDate.Text = Booking.StartDate.ToString("dd/MM/yyyy");
            txtAllVoucher.Text = Booking.VoucherCode;
            chkIsPaymentNeeded.Checked = Booking.IsPaymentNeeded;
            txtCustomerInfo.Text = Booking.Note;
            txtCancelledReason.Text = Booking.CancelledReason;
            litPax.Text = Booking.Pax.ToString();
            litCabins.Text = Booking.BookingRooms.Count.ToString();
            txtAgencyCode.Text = Booking.AgencyCode;
            ddlOptions.SelectedIndex = (int)Booking.TripOption;
            txtPickup.Text = Booking.PickupAddress;
            if (string.IsNullOrWhiteSpace(Booking.PickupTime))
            {
                if (Booking.Cruise.Code.Contains("OS") || Booking.Cruise.Code.Contains("ST"))
                {
                    Booking.PickupTime = "08:30";
                }
                else if (Booking.Cruise.Code.Contains("NCL"))
                {
                    Booking.PickupTime = "09:30";
                }
            }
            txtPickupTime.Text = Booking.PickupTime;
            txtSpecialRequest.Text = Booking.SpecialRequest;
            txtSpecialRequestRoom.Text = Booking.SpecialRequestRoom;
            chkInspection.Checked = Booking.Inspection;
            chkSpecial.Checked = Booking.Special;
            chkCharter.Checked = Booking.IsCharter;
            chkInvoice.Checked = Booking.HasInvoice;
            chkEarlyBird.Checked = Booking.IsEarlyBird;
            chkAnChay.Checked = Booking.CO_AN_CHAY;
            txtPenalty.Text = Booking.CancelPay.ToString();
            ddlCurrencies.SelectedValue = Convert.ToInt32(Booking.IsTotalUsd).ToString();
            txtCutOffDays.Text = Booking.CutOffDays.ToString();
            try
            {
                txtSeriesCode.Text = Booking?.Series?.SeriesCode;
            }
            catch
            {

            }

            try
            {
                cddlBooker.SelectedValue = Booking?.Booker?.Id.ToString();
            }
            catch { }

            try
            {
                txtDeadline.Text = Booking.Deadline.Value.ToString("dd/MM/yyyy HH:mm");
            }
            catch { }

            try
            {
                ddlTrips.SelectedValue = Booking.Trip.Id.ToString();
            }
            catch { }

            try
            {
                ddlCruises.SelectedValue = Booking.Cruise.Id.ToString();
            }
            catch { }

            string text = "";
            try
            {
                text += string.Format("Created by {0} at {1}", Booking.CreatedBy?.FullName ?? "",
                                     Booking.CreatedDate.ToString("dd/MM/yyyy HH:mm"));
            }
            catch { }

            try
            {
                text += string.Format(" and last edited by {0} at {1}", Booking.ModifiedBy?.FullName ?? "",
                                 Booking.ModifiedDate.ToString("dd/MM/yyyy HH:mm"));
            }
            catch { }


            try
            {
                ddlAgencies.SelectedValue = Booking.Agency.Id.ToString();
            }
            catch { }
            litCreated.Text = text;

            if (Booking.Cruise.CruiseType == Enums.CruiseType.Cabin)
            {
                rptRoomList.DataSource = Booking.BookingRooms;
                rptRoomList.DataBind();
            }

            if (Booking.Cruise.CruiseType == Enums.CruiseType.Seating)
            {
                rptAdults.DataSource = Booking.Customers.Where(x => x.Type == CustomerType.Adult);
                rptAdults.DataBind();

                rptChildren.DataSource = Booking.Customers.Where(x => x.Type == CustomerType.Children);
                rptChildren.DataBind();

                rptBabies.DataSource = Booking.Customers.Where(x => x.Type == CustomerType.Baby);
                rptBabies.DataBind();
            }
            NumberOfDay = Booking.Trip.NumberOfDay;
        }

        public void CustomerBirthdayDisplay()
        {
            var customersBirthday = new List<Customer>();
            foreach (var bookingRoom in Booking.BookingRooms)
            {
                foreach (var customer in bookingRoom.RealCustomers)
                {
                    DateTime? customerBirthday = null;
                    try
                    {
                        customerBirthday = customer.Birthday.Value;
                    }
                    catch { return; }

                    if (customerBirthday.Value.Day == Booking.StartDate.Day
                        && customerBirthday.Value.Month == Booking.StartDate.Month)
                    {
                        customersBirthday.Add(customer);
                    }
                }
            }

            if (customersBirthday.Count < 1)
                return;

            litInform.Text = "<i class='fa fa-lg fa-birthday-cake' aria-hidden='true'></i>  ";
            foreach (var customer in customersBirthday)
            {
                litInform.Text += customer.Fullname + ",";
            }
            litInform.Text += " have birthday on start date.";
        }

        public void AddRoomControlGenerate()
        {
            var haveRoomAvaiable = false;
            ddlRoomTypes.Items.Clear();

            var roomTypes = Module.RoomTypexGetAll();
            foreach (RoomTypex room in roomTypes)
            {
                if (room.Name == "Double" || room.Name == "Twin")
                    ddlRoomTypes.Items.Add(new ListItem(room.Name, room.Id.ToString()));
            }

        }

        public void TotalLockedDisplay()
        {
            var isLocked = Booking.LockIncome;
            var haveEditAfterLockPermission = PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.EDIT_AFTER_LOCK);
            var haveLockIncomePermission = PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.LOCK_INCOME);

            if (isLocked)
            {
                if (!haveEditAfterLockPermission)
                {
                    txtTotal.ReadOnly = true;
                    txtTotal.CssClass = txtTotal.CssClass + " total-locked ";
                    ddlCurrencies.Enabled = false;
                }
            }

            if (haveLockIncomePermission)
            {
                btnLockIncome.Visible = true;
                btnUnlockIncome.Visible = false;
                if (isLocked)
                {
                    btnLockIncome.Visible = false;
                    btnUnlockIncome.Visible = true;
                }
            }
        }

        public void SendEmailCancelled()
        {
            try
            {
                string content = "";
                using (StreamReader streamReader = new StreamReader(HostingEnvironment.MapPath("/Modules/Sails/Admin/EmailTemplate/CancelledBookingNotify.txt")))
                {
                    content = streamReader.ReadToEnd();
                };
                var appPath = string.Format("{0}://{1}{2}{3}",
                                 Request.Url.Scheme,
                                 Request.Url.Host,
                                 Request.Url.Port == 80
                                     ? string.Empty
                                     : ":" + Request.Url.Port,
                                 Request.ApplicationPath);
                content = content.Replace("{link}",
                    appPath + "Modules/Sails/Admin/BookingView.aspx?NodeId=1&SectionId=15&bi=" + Booking.Id);
                content = content.Replace("{bookingcode}", Booking.BookingIdOS);
                content = content.Replace("{agency}", Booking.Agency.Name);
                content = content.Replace("{startdate}", Booking.StartDate.ToString("dd/MM/yyyy"));
                content = content.Replace("{trip}", Booking.Trip.Name);
                content = content.Replace("{cruise}", Booking.Cruise.Name);
                content = content.Replace("{customernumber}", Booking.Pax.ToString());
                content = content.Replace("{roomnumber}", Booking.BookingRooms.Count.ToString());
                content = content.Replace("{submiter}", UserIdentity.FullName);
                content = content.Replace("{reason}", Booking.CancelledReason);
                MailAddress fromAddress = new MailAddress("no-reply@orientalsails.com", "Hệ Thống MO OrientalSails");
                MailMessage message = new MailMessage();
                message.From = fromAddress;
                message.To.Add("reservation@orientalsails.com");
                if (Booking.CreatedBy != null)
                {
                    if (Booking.CreatedBy.Email != null)
                    {
                        if (Booking.CreatedBy.Email != "reservation@orientalsails.com")
                        {
                            message.To.Add(Booking.CreatedBy.Email);
                        }
                    }
                }

                if (Booking.ModifiedBy != null)
                {
                    if (Booking.ModifiedBy.Email != null)
                    {
                        if (Booking.ModifiedBy.Email != Booking.CreatedBy.Email)
                        {
                            if (Booking.ModifiedBy.Email != "reservation@orientalsails.com")
                            {
                                message.To.Add(Booking.ModifiedBy.Email);
                            }
                        }
                    }
                }

                message.To.Add("nhan@orientalsails.com");
                message.To.Add("it2@atravelmate.com");
                message.Subject = "Thông báo hủy booking";
                message.Body = content;
                message.Bcc.Add("it2@atravelmate.com");
                EmailService.SendMessage(message);
            }
            catch (Exception)
            {
            }
        }

        public string PaxGetDetails()
        {
            return string.Format("Adults : {0}</br> Childs : {1}<br/> Baby : {2}", Booking.Adult, Booking.Child, Booking.Baby);
        }

        public string CabinGetDetails()
        {
            return Booking.RoomName;
        }

        public string UserGetUserLockIncomeDetails()
        {
            if (Booking.LockIncome)
                return string.Format(
                                "Locked (individual booking) by {0} at {1:dd/MM/yyyy HH:mm}", Booking.LockByString,
                                Booking.LockDate.HasValue ? Booking.LockDate.Value : Booking.EndDate.AddDays(1));

            return "";
        }

        public void BookingHistorySave()
        {
            var bookingHistory = new BookingHistory()
            {
                Booking = Booking,
                Date = DateTime.Now,
                User = CurrentUser,
                CabinNumber = Booking.BookingRooms.Count,
                CustomerNumber = Booking.Pax,
                StartDate = Booking.StartDate,
                Status = Booking.Status,
                Trip = Booking.Trip,
                Agency = Booking.Agency,
                Total = Booking.Total,
                TotalCurrency = Booking.IsTotalUsd ? "USD" : "VND",
                SpecialRequest = Booking.SpecialRequest,
                PickupAddress = Booking.PickupAddress,
            };
            Module.SaveOrUpdate(bookingHistory);
        }

        public SailsTrip GetTrip()
        {
            SailsTrip trip = null;
            try
            {
                trip = BookingViewBLL.TripGetById(Convert.ToInt32(ddlTrips.SelectedValue));
            }
            catch { }
            return trip;
        }

        public Cruise GetCruise()
        {
            Cruise cruise = null;
            try
            {
                cruise = BookingViewBLL.CruiseGetById(Convert.ToInt32(ddlCruises.SelectedValue));
            }
            catch { }
            return cruise;
        }

        public DateTime? GetStartDate()
        {
            DateTime? startDate = null;
            try
            {
                startDate = DateTime.ParseExact(txtStartDate.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch { }
            return startDate;
        }

        public void SaveExtraService()
        {
            var busTypeId = -1;
            try
            {
                busTypeId = Int32.Parse(Request.Form["radBusType"]);
            }
            catch { }
            Booking.Transfer_BusType = BookingViewBLL.BusTypeGetById(busTypeId);
            if (rbtTransferService_OneWay.Checked)
            {
                Booking.Transfer_Service = "One Way";
            }
            if (rbtTransferService_TwoWay.Checked)
            {
                Booking.Transfer_Service = "Two Way";
            }
            var transfer_DateTo = Booking.StartDate;
            try
            {
                transfer_DateTo = DateTime.ParseExact(txtTransfer_Dateto.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch { }
            var transfer_DateBack = Booking.EndDate;
            try
            {
                transfer_DateBack = DateTime.ParseExact(txtTransfer_Dateback.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch { }
            if (Booking.Transfer_Service == "One Way")
            {
                if (!String.IsNullOrEmpty(txtTransfer_Dateto.Text))
                {
                    Booking.Transfer_DateTo = transfer_DateTo;
                    Booking.Transfer_DateBack = null;
                }
                else
                    if (!String.IsNullOrEmpty(txtTransfer_Dateback.Text))
                {
                    Booking.Transfer_DateTo = null;
                    Booking.Transfer_DateBack = transfer_DateBack;
                }
            }
            if (Booking.Transfer_Service == "Two Way")
            {
                Booking.Transfer_DateBack = transfer_DateBack;
                Booking.Transfer_DateTo = transfer_DateTo;
            }
            Booking.Transfer_Note = txtTransfer_Note.Text;
            Booking.DIA_DIEM_TRANSFER = (TransferLocationType)Enum.ToObject(typeof(TransferLocationType), Int32.Parse(ddlTransferLocation.SelectedValue));
            Booking.HasInvoice = chkInvoice.Checked;
            BookingViewBLL.BookingSaveOrUpdate(Booking);
        }

        public void DeleteExtraService()
        {
            Booking.Transfer_BusType = null;
            Booking.Transfer_DateTo = null;
            Booking.Transfer_DateBack = null;
            Booking.Transfer_Note = null;
            Booking.Transfer_Service = null;
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

        protected void btnDeleteAllRoomNA_OnClick(object sender, EventArgs e)
        {
            var list = new List<BookingRoom>(Booking.BookingRooms);
            if (list.Count > 0)
            {
                foreach (BookingRoom bookingRoom in list)
                {
                    if (bookingRoom.Room == null)
                    {
                        var oldRoom = Module.BookingRoomGetById(bookingRoom.Id);
                        Module.Delete(oldRoom);
                    }
                }
            }
            Response.Redirect(Request.RawUrl);
        }

        protected void btnDeleteRoomSelect_OnClick(object sender, EventArgs e)
        {
            foreach (RepeaterItem item in rptRoomList.Items)
            {
                var hiddenBookingRoomId = item.FindControl("hiddenBookingRoomId") as HiddenField;
                var chkDeleteRoom = item.FindControl("chkDeleteRoom") as CheckBox;
                if (chkDeleteRoom != null && !string.IsNullOrWhiteSpace(hiddenBookingRoomId.Value) && chkDeleteRoom.Checked)
                {
                    BookingRoom bookingRoom = BookingViewBLL.BookingRoomGetById(Convert.ToInt32(hiddenBookingRoomId.Value));
                    Booking.BookingRooms.Remove(bookingRoom);
                    BookingViewBLL.BookingRoomDelete(bookingRoom);
                }
            }
            Response.Redirect(Request.RawUrl);
        }

        protected void btnAddAdult_Click(object sender, EventArgs e)
        {
            Customer customer = new Customer();
            customer.Type = CustomerType.Adult;
            customer.Booking = Booking;
            Module.SaveOrUpdate(customer);
            Response.Redirect(Request.RawUrl);
        }

        protected void btnAddChild_Click(object sender, EventArgs e)
        {
            Customer customer = new Customer();
            customer.Type = CustomerType.Children;
            customer.Booking = Booking;
            Module.SaveOrUpdate(customer);
            Response.Redirect(Request.RawUrl);
        }

        protected void btnAddBaby_Click(object sender, EventArgs e)
        {
            Customer customer = new Customer();
            customer.Type = CustomerType.Baby;
            customer.Booking = Booking;
            Module.SaveOrUpdate(customer);
            Response.Redirect(Request.RawUrl);
        }

        protected void rptBabies_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            switch (e.CommandName.ToLower())
            {
                case "delete":
                    var customer = Module.GetById<Customer>(Convert.ToInt32(e.CommandArgument));
                    Module.Delete(customer);
                    break;

            }

            Response.Redirect(Request.RawUrl);
        }

        protected void rptChildren_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            switch (e.CommandName.ToLower())
            {
                case "delete":
                    var customer = Module.GetById<Customer>(Convert.ToInt32(e.CommandArgument));
                    Module.Delete(customer);
                    break;

            }

            Response.Redirect(Request.RawUrl);
        }

        protected void rptAdults_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            switch (e.CommandName.ToLower())
            {
                case "delete":
                    var customer = Module.GetById<Customer>(Convert.ToInt32(e.CommandArgument));
                    Module.Delete(customer);
                    break;

            }

            Response.Redirect(Request.RawUrl);
        }

        protected void rptSalesPriceInput_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            var txtNumberOfRoomsPrice = (HtmlInputText)e.Item.FindControl("txtNumberOfRoomsPrice");
            var txtNumberOfAddAdultPrice = (HtmlInputText)e.Item.FindControl("txtNumberOfAddAdultPrice");
            var txtNumberOfAddChildPrice = (HtmlInputText)e.Item.FindControl("txtNumberOfAddChildPrice");
            var txtNumberOfAddBabyPrice = (HtmlInputText)e.Item.FindControl("txtNumberOfAddBabyPrice");
            var txtNumberOfExtrabedPrice = (HtmlInputText)e.Item.FindControl("txtNumberOfExtrabedPrice");
            var txtNumberOfRoomsSinglePrice = (HtmlInputText)e.Item.FindControl("txtNumberOfRoomsSinglePrice");

            var roomPriceSalesInput = (RoomPriceSalesInput)e.Item.DataItem;
            var roomPrice = Booking.BookingRoomPrices.Where(x => x.RoomClass.Id == roomPriceSalesInput.RoomClassId && x.RoomType.Id == roomPriceSalesInput.RoomTypeId).FirstOrDefault();
            txtNumberOfRoomsPrice.Attributes.Add("ng-model", "txtNumberOfRoomsPrice" + roomPriceSalesInput.RoomClassId.ToString() + roomPriceSalesInput.RoomTypeId.ToString());
            txtNumberOfAddAdultPrice.Attributes.Add("ng-model", "txtNumberOfAddAdultPrice" + roomPriceSalesInput.RoomClassId.ToString() + roomPriceSalesInput.RoomTypeId.ToString());
            txtNumberOfAddChildPrice.Attributes.Add("ng-model", "txtNumberOfAddChildPrice" + roomPriceSalesInput.RoomClassId.ToString() + roomPriceSalesInput.RoomTypeId.ToString());
            txtNumberOfAddBabyPrice.Attributes.Add("ng-model", "txtNumberOfAddBabyPrice" + roomPriceSalesInput.RoomClassId.ToString() + roomPriceSalesInput.RoomTypeId.ToString());
            txtNumberOfExtrabedPrice.Attributes.Add("ng-model", "txtNumberOfExtrabedPrice" + roomPriceSalesInput.RoomClassId.ToString() + roomPriceSalesInput.RoomTypeId.ToString());
            txtNumberOfRoomsSinglePrice.Attributes.Add("ng-model", "txtNumberOfRoomsSinglePrice" + roomPriceSalesInput.RoomClassId.ToString() + roomPriceSalesInput.RoomTypeId.ToString());

            if (roomPrice == null)
            {
                txtNumberOfRoomsPrice.Attributes.Add("ng-init", String.Format("txtNumberOfRoomsPrice{0}{1}=0;numberOfRooms{0}{1}={2}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPriceSalesInput.NumberOfRooms.ToString()));
                txtNumberOfRoomsSinglePrice.Attributes.Add("ng-init", String.Format("txtNumberOfRoomsSinglePrice{0}{1}=0;numberOfRoomsSingle{0}{1}={2}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPriceSalesInput.NumberOfRoomsSingle.ToString()));
                txtNumberOfAddAdultPrice.Attributes.Add("ng-init", String.Format("txtNumberOfAddAdultPrice{0}{1}=0;numberOfAddAdult{0}{1}={2}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPriceSalesInput.NumberOfAddAdult.ToString()));
                txtNumberOfAddChildPrice.Attributes.Add("ng-init", String.Format("txtNumberOfAddChildPrice{0}{1}=0;numberOfAddChild{0}{1}={2}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPriceSalesInput.NumberOfAddChild.ToString()));
                txtNumberOfAddBabyPrice.Attributes.Add("ng-init", String.Format("txtNumberOfAddBabyPrice{0}{1}=0;numberOfAddBaby{0}{1}={2}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPriceSalesInput.NumberOfAddBaby.ToString()));
                txtNumberOfExtrabedPrice.Attributes.Add("ng-init", String.Format("txtNumberOfExtrabedPrice{0}{1}=0;numberOfExtrabed{0}{1}={2}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPriceSalesInput.NumberOfExtrabed.ToString()));
            }
            else
            {
                txtNumberOfRoomsPrice.Attributes.Add("ng-init", String.Format("txtNumberOfRoomsPrice{0}{1}={2};numberOfRooms{0}{1}={3}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPrice.PriceOfRoom.ToString(), roomPriceSalesInput.NumberOfRooms.ToString()));
                txtNumberOfRoomsSinglePrice.Attributes.Add("ng-init", String.Format("txtNumberOfRoomsSinglePrice{0}{1}={2};numberOfRoomsSingle{0}{1}={3}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPrice.PriceOfRoomSingle.ToString(), roomPriceSalesInput.NumberOfRoomsSingle.ToString()));
                txtNumberOfAddAdultPrice.Attributes.Add("ng-init", String.Format("txtNumberOfAddAdultPrice{0}{1}={2};numberOfAddAdult{0}{1}={3}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPrice.PriceOfAddAdult.ToString(), roomPriceSalesInput.NumberOfAddAdult.ToString()));
                txtNumberOfAddChildPrice.Attributes.Add("ng-init", String.Format("txtNumberOfAddChildPrice{0}{1}={2};numberOfAddChild{0}{1}={3}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPrice.PriceOfAddChild.ToString(), roomPriceSalesInput.NumberOfAddChild.ToString()));
                txtNumberOfAddBabyPrice.Attributes.Add("ng-init", String.Format("txtNumberOfAddBabyPrice{0}{1}={2};numberOfAddBaby{0}{1}={3}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPrice.PriceOfAddBaby.ToString(), roomPriceSalesInput.NumberOfAddBaby.ToString()));
                txtNumberOfExtrabedPrice.Attributes.Add("ng-init", String.Format("txtNumberOfExtrabedPrice{0}{1}={2};numberOfExtrabed{0}{1}={3}", roomPriceSalesInput.RoomClassId.ToString(), roomPriceSalesInput.RoomTypeId.ToString(), roomPrice.PriceOfExtrabed.ToString(), roomPriceSalesInput.NumberOfExtrabed.ToString()));
            }
        }
    }
}