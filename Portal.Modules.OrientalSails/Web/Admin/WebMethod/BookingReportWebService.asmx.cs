﻿using CMS.Core.Domain;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Portal.Modules.OrientalSails.BusinessLogic;
using Portal.Modules.OrientalSails.BusinessLogic.Share;
using Portal.Modules.OrientalSails.DataTransferObject;
using Portal.Modules.OrientalSails.Domain;
using Portal.Modules.OrientalSails.Enums;
using Portal.Modules.OrientalSails.Web.Admin.Utilities;
using Portal.Modules.OrientalSails.Web.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace Portal.Modules.OrientalSails.Web.Admin.WebMethod
{
    /// <summary>
    /// Summary description for BookingReportWebService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [System.Web.Script.Services.ScriptService]
    public class BookingReportWebService : System.Web.Services.WebService
    {
        private PermissionBLL permissionBLL;
        private UserBLL userBLL;
        private BookingReportBLL bookingReportBLL;

        public PermissionBLL PermissionBLL
        {
            get
            {
                if (permissionBLL == null)
                    permissionBLL = new PermissionBLL();
                return permissionBLL;
            }
        }
        public bool CanViewSpecialRequestFood
        {
            get
            {
                return PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.SPECIALREQUEST_FOOD);
            }
        }

        public bool CanViewSpecialRequestRoom
        {
            get
            {
                return PermissionBLL.UserCheckPermission(CurrentUser.Id, (int)PermissionEnum.SPECIALREQUEST_ROOM);
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

        public User CurrentUser
        {
            get
            {
                return UserBLL.UserGetCurrent();
            }
        }

        public BookingReportBLL BookingReportBLL
        {
            get
            {
                if (bookingReportBLL == null)
                {
                    bookingReportBLL = new BookingReportBLL();
                }
                return bookingReportBLL;
            }
        }

        public new void Dispose()
        {
            if (bookingReportBLL != null)
            {
                bookingReportBLL.Dispose();
                bookingReportBLL = null;
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
        [WebMethod]
        public void Save(List<CruiseDTO> listCruiseExpenseDTO)
        {
            listCruiseExpenseDTO.ForEach(x =>
            {
                x.ListGuideExpenseDTO.ForEach(y =>
                {
                    var guide = BookingReportBLL.AgencyGetById(y.GuideId);
                    var cruise = BookingReportBLL.CruiseGetById(x.Id);
                    DateTime? date = null;
                    try
                    {
                        date = DateTime.ParseExact(y.Date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    }
                    catch { }
                    var _operator = BookingReportBLL.UserGetById(y.Operator_UserId);
                    var cost = 0.0;
                    try
                    {
                        cost = Double.Parse(y.Cost);
                    }
                    catch { }
                    var expense = BookingReportBLL.ExpenseGetById(y.Id);
                    if (expense == null || expense.Id <= 0)
                    {
                        expense = new Expense();
                        expense.Operator = _operator;
                    }
                    expense.CurrentUser = CurrentUser;
                    expense.Guide = guide;
                    expense.Cruise = cruise;
                    expense.Cost = cost;
                    expense.Date = date != null ? date.Value : DateTime.Now.Date;
                    expense.Type = "Guide";
                    expense.LockStatus = y.LockStatus;
                    BookingReportBLL.ExpenseSaveOrUpdate(expense);
                    var mustChangeOperator = false;
                    expense.ListPendingExpenseHistory.ForEach(z =>
                    {
                        BookingReportBLL.ExpenseHistorySaveOrUpdate(z);
                        mustChangeOperator = true;
                    });
                    if (mustChangeOperator)
                    {
                        expense.Operator = CurrentUser;
                    }
                    var listCostType = BookingReportBLL.CostTypeGetAll().List();
                    var expenseTypeNull = BookingReportBLL.ExpenseGetAllByCriterion(cruise.Id, date).Where(z => z.Type == null).SingleOrDefault();
                    var expenseService = BookingReportBLL.ExpenseServiceGetAllByCriterion(expense.Id).SingleOrDefault();
                    if (expenseService == null || expenseService.Id <= 0)
                    {
                        expenseService = new ExpenseService();
                    }
                    expenseService.Cost = expense.Cost;
                    expenseService.Name = expense.Guide != null ? expense.Guide.Name : "";
                    expenseService.Supplier = expense.Guide;
                    expenseService.Type = listCostType.Where(z => z.Name == "Guide").FirstOrDefault();
                    expenseService.Expense = expenseTypeNull;
                    expenseService.ExpenseIdRef = expense.Id;
                    BookingReportBLL.ExpenseServiceSaveOrUpdate(expenseService);
                });
                x.ListOthersExpenseDTO.ForEach(y =>
                {
                    var cruise = BookingReportBLL.CruiseGetById(x.Id);
                    DateTime? date = null;
                    try
                    {
                        date = DateTime.ParseExact(y.Date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    }
                    catch { }
                    var _operator = BookingReportBLL.UserGetById(y.Operator_UserId);
                    var cost = 0.0;
                    try
                    {
                        cost = Double.Parse(y.Cost);
                    }
                    catch { }
                    var expense = BookingReportBLL.ExpenseGetById(y.Id);
                    if (expense == null || expense.Id <= 0)
                    {
                        expense = new Expense();
                        expense.Operator = _operator;
                    }
                    expense.CurrentUser = CurrentUser;
                    expense.Name = y.Name;
                    expense.Phone = y.Phone;
                    expense.Cruise = cruise;
                    expense.Cost = cost;
                    expense.Date = date != null ? date.Value : DateTime.Now.Date;
                    expense.Type = "Others";
                    expense.LockStatus = y.LockStatus;
                    BookingReportBLL.ExpenseSaveOrUpdate(expense);
                    var mustChangeOperator = false;
                    expense.ListPendingExpenseHistory.ForEach(z =>
                    {
                        BookingReportBLL.ExpenseHistorySaveOrUpdate(z);
                        mustChangeOperator = true;
                    });
                    if (mustChangeOperator)
                    {
                        expense.Operator = CurrentUser;
                    }
                    var listCostType = BookingReportBLL.CostTypeGetAll().List();
                    var expenseTypeNull = BookingReportBLL.ExpenseGetAllByCriterion(cruise.Id, date).Where(z => z.Type == null).SingleOrDefault();
                    var expenseService = BookingReportBLL.ExpenseServiceGetAllByCriterion(expense.Id).SingleOrDefault();
                    if (expenseService == null || expenseService.Id <= 0)
                    {
                        expenseService = new ExpenseService();
                    }
                    expenseService.Cost = expense.Cost;
                    expenseService.Name = expense.Name;
                    expenseService.Type = listCostType.Where(z => z.Name == "Others").FirstOrDefault();
                    expenseService.Expense = expenseTypeNull;
                    expenseService.ExpenseIdRef = expense.Id;
                    BookingReportBLL.ExpenseServiceSaveOrUpdate(expenseService);
                });
                x.ListDeletedGuideExpenseDTO.ForEach(y =>
                {
                    var deletedGuideExpense = BookingReportBLL.ExpenseGetById(y.Id);
                    if (deletedGuideExpense != null)
                    {
                        if (deletedGuideExpense.Id > 0)
                        {
                            BookingReportBLL.ExpenseDelete(deletedGuideExpense);
                        }
                    }
                });
                x.ListDeletedOthersExpenseDTO.ForEach(y =>
                {
                    var deletedOthersExpense = BookingReportBLL.ExpenseGetById(y.Id);
                    if (deletedOthersExpense != null)
                    {
                        if (deletedOthersExpense.Id > 0)
                        {
                            BookingReportBLL.ExpenseDelete(deletedOthersExpense);
                        }
                    }
                });
            });
            Dispose();
        }

        public List<ExpenseDTO> GetListGuideExpenseDTO(int ci, string d)
        {
            var cruiseId = ci;
            DateTime? date = null;
            try
            {
                date = DateTime.ParseExact(d, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch { }
            var listExpense = BookingReportBLL.ExpenseGetAllByCriterion(cruiseId, date).List();
            var listGuideExpense = listExpense.Where(x => x.Type == "Guide").ToList();
            var listGuideExpenseDTO = new List<ExpenseDTO>();
            listGuideExpense.ForEach(x =>
            {
                var guideExpenseDTO = new ExpenseDTO()
                {
                    Id = x.Id,
                    GuideId = x.Guide?.Id ?? -1,
                    GuideName = x.Guide?.Name ?? "",
                    GuidePhone = x.Guide?.Phone ?? "",
                    Cost = x.Cost.ToString("#,##0.##"),
                    CruiseId = cruiseId,
                    Date = date?.ToString("dd/MM/yyyy") ?? "",
                    Operator_FullName = x.Operator?.FullName ?? "",
                    Operator_Phone = x.Operator?.Phone ?? "",
                    Operator_UserId = x.Operator?.Id ?? -1,
                    LockStatus = x.LockStatus,
                };
                listGuideExpenseDTO.Add(guideExpenseDTO);
            });
            Dispose();
            return listGuideExpenseDTO;
        }

        public List<ExpenseDTO> GetListOthersExpenseDTO(int ci, string d)
        {
            var cruiseId = ci;
            DateTime? date = null;
            try
            {
                date = DateTime.ParseExact(d, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch { }
            var listExpense = BookingReportBLL.ExpenseGetAllByCriterion(cruiseId, date).List();
            var listOthersExpense = listExpense.Where(x => x.Type == "Others").ToList();
            var listOthersExpenseDTO = new List<ExpenseDTO>();
            listOthersExpense.ForEach(x =>
            {
                var guideExpenseDTO = new ExpenseDTO()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Phone = x.Phone,
                    Cost = x.Cost.ToString("#,##0.##"),
                    CruiseId = cruiseId,
                    Date = date.HasValue ? date.Value.ToString("dd/MM/yyyy") : "",
                    Operator_FullName = x.Operator != null ? x.Operator.FullName : "",
                    Operator_Phone = x.Operator != null ? x.Operator.Phone : "",
                    Operator_UserId = x.Operator != null ? x.Operator.Id : -1,
                    LockStatus = x.LockStatus,
                };
                listOthersExpenseDTO.Add(guideExpenseDTO);
            });
            Dispose();
            return listOthersExpenseDTO;
        }

        [WebMethod]
        public string GetListCruiseExpenseDTO(string d, int ci)
        {
            var listCruise = BookingReportBLL.CruiseGetAll();
            if (ci > 0)
            {
                listCruise = listCruise.Where(x => x.Id == ci).ToList();
            }
            var listCruiseExpenseDTO = new List<CruiseDTO>();
            foreach (var cruise in listCruise)
            {
                var listGuideExpenseDTO = GetListGuideExpenseDTO(cruise.Id, d);
                var listOthersExpenseDTO = GetListOthersExpenseDTO(cruise.Id, d);
                var cruiseExpenseDTO = new CruiseDTO()
                {
                    Id = cruise.Id,
                    Name = cruise.Name,
                    ListGuideExpenseDTO = listGuideExpenseDTO,
                    ListOthersExpenseDTO = listOthersExpenseDTO,
                    ListDeletedGuideExpenseDTO = new List<ExpenseDTO>(),
                    ListDeletedOthersExpenseDTO = new List<ExpenseDTO>(),
                };
                listCruiseExpenseDTO.Add(cruiseExpenseDTO);
            }
            Dispose();
            return JsonConvert.SerializeObject(listCruiseExpenseDTO);
        }
        [WebMethod]
        public string GetListAllCruiseExpenseDTO(string d)
        {
            return GetListCruiseExpenseDTO(d, -1);
        }

        [WebMethod]
        public void LockDate(List<CruiseDTO> listCruiseExpenseDTO)
        {
            listCruiseExpenseDTO.ForEach(x =>
            {
                x.ListGuideExpenseDTO.ForEach(y =>
                {
                    var expense = BookingReportBLL.ExpenseGetById(y.Id);
                    if (expense != null && expense.Id > 0)
                    {
                        expense.LockStatus = "Locked";
                        BookingReportBLL.ExpenseSaveOrUpdate(expense);
                    }
                });
                x.ListOthersExpenseDTO.ForEach(y =>
                {
                    var expense = BookingReportBLL.ExpenseGetById(y.Id);
                    if (expense != null && expense.Id > 0)
                    {
                        expense.LockStatus = "Locked";
                        BookingReportBLL.ExpenseSaveOrUpdate(expense);
                    }
                });
            });
            Dispose();
        }

        [WebMethod]
        public void UnlockDate(List<CruiseDTO> listCruiseExpenseDTO)
        {
            listCruiseExpenseDTO.ForEach(x =>
            {
                x.ListGuideExpenseDTO.ForEach(y =>
                {
                    var expense = BookingReportBLL.ExpenseGetById(y.Id);
                    if (expense != null && expense.Id > 0)
                    {
                        expense.LockStatus = "Unlocked";
                        BookingReportBLL.ExpenseSaveOrUpdate(expense);
                    }
                });
                x.ListOthersExpenseDTO.ForEach(y =>
                {
                    var expense = BookingReportBLL.ExpenseGetById(y.Id);
                    if (expense != null && expense.Id > 0)
                    {
                        expense.LockStatus = "Unlocked";
                        BookingReportBLL.ExpenseSaveOrUpdate(expense);
                    }
                });
            });
            Dispose();
        }

        [WebMethod]
        public void ExportTourByCruiseAndGuide(string ci, string gi, string d, string ob, string op)
        {
            var cruiseId = -1;
            try
            {
                cruiseId = Int32.Parse(ci);
            }
            catch { }
            var cruise = BookingReportBLL.CruiseGetById(cruiseId);
            var guideId = -1;
            try
            {
                guideId = Int32.Parse(gi);
            }
            catch { }
            var guide = BookingReportBLL.Guide_AgencyGetById(guideId);
            if (guide == null)
            {
                guide = new Agency();
            }
            DateTime? date = null;
            try
            {
                date = DateTime.ParseExact(d, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch
            {
                date = DateTime.Today;
            }
            var operationName = ob;
            var operationPhone = op;
            var listBooking = BookingReportBLL.BookingGetByCriterion(date, cruise, CurrentUser).Where(x => x.Status == StatusType.Approved)
                .List();
            using (var memoryStream = new MemoryStream())
            {
                using (var excelPackage = new ExcelPackage(new FileInfo(Server.MapPath("/Modules/Sails/Admin/ExportTemplates/TourCommand.xlsx"))))
                {
                    var sheet = excelPackage.Workbook.Worksheets.Copy("Tour Command", "TC" + "-" + cruise.Code + "-" + guide.Name);
                    sheet.Cells["I1"].Value = cruise.Name;
                    sheet.Cells["E1"].Value = (date.HasValue ? date.Value.ToLongDateString() : "");
                    sheet.Cells["B3"].Value = "Guide:";
                    sheet.Cells["C3"].Value = (guide != null ? guide.Name : "");
                    sheet.Cells["D3"].Value = "Mob:";
                    sheet.Cells["E3"].Value = (guide != null ? NumberUtil.FormatPhoneNumber(guide.Phone) : "");
                    sheet.Cells["B5"].Value = "Opt:";
                    sheet.Cells["C5"].Value = operationName;
                    sheet.Cells["D5"].Value = "Mob:";
                    sheet.Cells["E5"].Value = NumberUtil.FormatPhoneNumber(operationPhone);
                    //Tìm driver theo booking
                    var listBusByDate = new List<BusByDate>();
                    foreach (var booking in listBooking)
                    {
                        var busByDate = booking.ListBookingBusByDate.Where(x => x.BusByDate != null && x.BusByDate.Date == date)
                            .Select(x => x.BusByDate)
                            .FirstOrDefault();
                        if (busByDate != null) { listBusByDate.Add(busByDate); }
                    }
                    //Điền driver vào lệnh điều tour
                    var templateDriverRow = 4;
                    sheet.Cells[templateDriverRow, 2].Value = "Driver:";
                    sheet.InsertRow(templateDriverRow, listBusByDate.Count - 1, templateDriverRow);
                    FillDriver(listBusByDate, sheet, ref templateDriverRow);
                    //--
                    //Export booking trong ngày
                    int titleRow = templateDriverRow + 1;
                    int templateRow = templateDriverRow + 3;
                    int currentRow = templateRow + 1;
                    int totalRow = templateRow + listBooking.Count();
                    int index = 1;
                    sheet.InsertRow(currentRow, listBooking.Count, currentRow - 1);
                    if (CanViewSpecialRequestFood && CanViewSpecialRequestFood)
                    {
                        sheet.Cells[titleRow, 9, titleRow + 1, 9].Merge = true;
                        sheet.Cells[titleRow, 10, titleRow + 1, 10].Merge = true;
                        sheet.Cells[titleRow, 9].Value = "Special Request Food";
                        sheet.Cells[titleRow, 10].Value = "Special Request Room";
                    }
                    else
                    {
                        if (CanViewSpecialRequestFood)
                        {
                            sheet.Cells[titleRow, 9, titleRow + 1, 10].Merge = true;
                            sheet.Cells[titleRow, 9].Value = "Special Request Food";
                        }

                        if (CanViewSpecialRequestRoom)
                        {
                            sheet.Cells[titleRow, 9, titleRow + 1, 10].Merge = true;
                            sheet.Cells[titleRow, 9].Value = "Special Request Room";
                        }
                    }
                    for (int i = 0; i < listBooking.Count; i++)
                    {
                        var booking = listBooking[i] as Booking;
                        if (booking != null)
                        {
                            var name = booking.CustomerNameFull.Replace("<br/>", "\r\n").ToUpper();
                            sheet.Cells[currentRow, 1].Value = index;
                            sheet.Cells[currentRow, 2, currentRow, 3].Merge = true;
                            sheet.Cells[currentRow, 2].Value = name;
                            sheet.Cells[currentRow, 4].Value = booking.Adult;
                            sheet.Cells[currentRow, 5].Value = booking.Child;
                            sheet.Cells[currentRow, 6].Value = booking.Baby;
                            sheet.Cells[currentRow, 7].Value = booking.Trip.TripCode;
                            sheet.Cells[currentRow, 8].Value = booking.PickupAddress;
                            sheet.Cells[currentRow, 11].Value = "OS" + booking.Id;
                            sheet.Cells[currentRow, 26].Value = name;//Work around cho cột merged name không hiển thị hết khi nội dung quá dài  
                            if (CanViewSpecialRequestFood && CanViewSpecialRequestFood)
                            {
                                sheet.Cells[currentRow, 9].Value = booking.SpecialRequest;
                                sheet.Cells[currentRow, 10].Value = booking.SpecialRequestRoom;
                            }
                            else
                            {
                                if (CanViewSpecialRequestFood)
                                {
                                    sheet.Cells[currentRow, 9, currentRow, 10].Merge = true;
                                    sheet.Cells[currentRow, 9].Value = booking.SpecialRequest;
                                }

                                if (CanViewSpecialRequestRoom)
                                {
                                    sheet.Cells[currentRow, 9, currentRow, 10].Merge = true;
                                    sheet.Cells[currentRow, 9].Value = booking.SpecialRequestRoom;
                                }
                            }
                            currentRow++;
                            index++;
                        }
                    }
                    sheet.DeleteRow(templateRow);
                    sheet.Cells[totalRow, 4].Value = listBooking.Sum(x => x.Adult);
                    sheet.Cells[totalRow, 5].Value = listBooking.Sum(x => x.Child);
                    sheet.Cells[totalRow, 6].Value = listBooking.Sum(x => x.Baby);
                    //--
                    //Export booking đi 2 ngày bắt đầu từ ngày hôm trước
                    listBooking = BookingReportBLL.BookingGetByCriterion(date.Value.AddDays(-1), cruise, CurrentUser)
                        .Where(x => x.Status == StatusType.Approved)
                        .List()
                        .Where(x => x.Trip.NumberOfDay > 2)
                        .ToList();
                    titleRow = currentRow + 2;
                    templateRow = currentRow + 4;
                    currentRow = templateRow + 1;
                    totalRow = templateRow + listBooking.Count();
                    index = 1;
                    sheet.InsertRow(currentRow, listBooking.Count, currentRow - 1);
                    if (CanViewSpecialRequestFood && CanViewSpecialRequestFood)
                    {
                        sheet.Cells[titleRow, 9, titleRow + 1, 9].Merge = true;
                        sheet.Cells[titleRow, 10, titleRow + 1, 10].Merge = true;
                        sheet.Cells[titleRow, 9].Value = "Special Request Food";
                        sheet.Cells[titleRow, 10].Value = "Special Request Room";
                    }
                    else
                    {
                        if (CanViewSpecialRequestFood)
                        {
                            sheet.Cells[titleRow, 9, titleRow + 1, 10].Merge = true;
                            sheet.Cells[titleRow, 9].Value = "Special Request Food";
                        }

                        if (CanViewSpecialRequestRoom)
                        {
                            sheet.Cells[titleRow, 9, titleRow + 1, 10].Merge = true;
                            sheet.Cells[titleRow, 9].Value = "Special Request Room";
                        }
                    }
                    for (int i = 0; i < listBooking.Count; i++)
                    {
                        var booking = listBooking[i] as Booking;
                        if (booking != null)
                        {
                            var name = booking.CustomerNameFull.Replace("<br/>", "\r\n").ToUpper();
                            sheet.Cells[currentRow, 1].Value = index;
                            sheet.Cells[currentRow, 2, currentRow, 3].Merge = true;
                            sheet.Cells[currentRow, 2].Value = name;
                            sheet.Cells[currentRow, 4].Value = booking.Adult;
                            sheet.Cells[currentRow, 5].Value = booking.Child;
                            sheet.Cells[currentRow, 6].Value = booking.Baby;
                            sheet.Cells[currentRow, 7].Value = booking.Trip.TripCode;
                            sheet.Cells[currentRow, 8].Value = booking.PickupAddress;
                            sheet.Cells[currentRow, 11].Value = "OS" + booking.Id;
                            sheet.Cells[currentRow, 26].Value = name;//Work around cho cột merged name không hiển thị hết khi nội dung quá dài
                            if (CanViewSpecialRequestFood && CanViewSpecialRequestFood)
                            {
                                sheet.Cells[currentRow, 9].Value = booking.SpecialRequest;
                                sheet.Cells[currentRow, 10].Value = booking.SpecialRequestRoom;
                            }
                            else
                            {
                                if (CanViewSpecialRequestFood)
                                {
                                    sheet.Cells[currentRow, 9, currentRow, 10].Merge = true;
                                    sheet.Cells[currentRow, 9].Value = booking.SpecialRequest;
                                }

                                if (CanViewSpecialRequestRoom)
                                {
                                    sheet.Cells[currentRow, 9, currentRow, 10].Merge = true;
                                    sheet.Cells[currentRow, 9].Value = booking.SpecialRequestRoom;
                                }
                            }
                            currentRow++;
                            index++;
                        }
                    }
                    sheet.DeleteRow(templateRow);
                    sheet.Cells[totalRow, 4].Value = listBooking.Sum(x => x.Adult);
                    sheet.Cells[totalRow, 5].Value = listBooking.Sum(x => x.Child);
                    sheet.Cells[totalRow, 6].Value = listBooking.Sum(x => x.Baby);
                    //--
                    excelPackage.Workbook.Worksheets.Delete("Tour Command");
                    excelPackage.SaveAs(memoryStream);
                }
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.ContentEncoding = System.Text.Encoding.UTF8;
                HttpContext.Current.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                HttpContext.Current.Response.BinaryWrite(memoryStream.ToArray());
                HttpContext.Current.Response.Flush();
                HttpContext.Current.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            Dispose();
        }
        private void FillDriver(IList<BusByDate> listBusByDate, ExcelWorksheet sheet, ref int currentRow)
        {
            for (var i = 0; i < listBusByDate.Count; i++)
            {
                //Nếu là Driver thứ 2 trở đi xóa chữ "Driver:"
                if (i == 0) { sheet.Cells[currentRow, 2].Value = "Driver:"; }
                else { sheet.Cells[currentRow, 2].Value = ""; }
                //--
                sheet.Cells[currentRow, 3].Value = listBusByDate[i].Driver_Name;
                sheet.Cells[currentRow, 4].Value = "Mob:";
                sheet.Cells[currentRow, 5].Value = NumberUtil.FormatPhoneNumber(listBusByDate[i].Driver_Phone);
                ++currentRow;
            }
        }
        private void FillGuide(IList<Agency> listGuide, ExcelWorksheet sheet, ref int currentRow)
        {
            for (var i = 0; i < listGuide.Count; i++)
            {
                if (listGuide[i] == null) continue;
                //Nếu là Guide thứ 2 trở đi xóa chữ "Guide:"
                if (i == 0) { sheet.Cells[currentRow, 2].Value = "Guide:"; }
                else { sheet.Cells[currentRow, 2].Value = ""; }
                //--
                sheet.Cells[currentRow, 3].Value = listGuide[i].Name;
                sheet.Cells[currentRow, 7].Value = "Mob:";
                sheet.Cells[currentRow, 8].Value = NumberUtil.FormatPhoneNumber(listGuide[i].Phone);
                ++currentRow;
            }
        }
        private void FillOpt(List<User> listOpt, ExcelWorksheet sheet, ref int currentRow)
        {
            for (var i = 0; i < listOpt.Count; i++)
            {
                //Nếu là Opt thứ 2 trở đi xóa chữ "Opt:"
                if (i == 0) { sheet.Cells[currentRow, 2].Value = "Opt:"; }
                else { sheet.Cells[currentRow, 2].Value = ""; }
                //--
                sheet.Cells[currentRow, 3].Value = listOpt[i].FullName;
                sheet.Cells[currentRow, 7].Value = "Mob:";
                sheet.Cells[currentRow, 8].Value = NumberUtil.FormatPhoneNumber(listOpt[i].Phone);
                ++currentRow;
            }
        }

        [WebMethod]
        public void ExportTourAll(string d)
        {
            DateTime? date = null;
            try
            {
                date = DateTime.ParseExact(d, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            catch
            {
                date = DateTime.Today;
            }
            using (var memoryStream = new MemoryStream())
            {
                var excelPackage = new ExcelPackage(new FileInfo(Server.MapPath("/Modules/Sails/Admin/ExportTemplates/TourCommand.xlsx")));
                var listCruise = BookingReportBLL.CruiseGetAll();
                foreach (var cruise in listCruise)
                {
                    if (cruise.Code != "VD")
                    {
                        ExportTourAllCabinCruise(ref excelPackage, date, cruise);
                    }
                    else
                    {
                        ExportTourAllVDreamCruise(ref excelPackage, date, cruise);
                    }
                }

                excelPackage.Workbook.Worksheets.Delete("Tour Command");
                excelPackage.SaveAs(memoryStream);
                excelPackage = null;
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.ContentEncoding = System.Text.Encoding.UTF8;
                HttpContext.Current.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                HttpContext.Current.Response.BinaryWrite(memoryStream.ToArray());
                HttpContext.Current.Response.Flush();
                HttpContext.Current.Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();

                Dispose();
            }
        }


        public void ExportTourAllCabinCruise(ref ExcelPackage excelPackage, DateTime? date, Cruise cruise)
        {
            var sheet = excelPackage.Workbook.Worksheets.Copy("Tour Command", "TC" + "-" + cruise.Code);
            sheet.Cells["I1"].Value = cruise.Name;
            sheet.Cells["E1"].Value = (date.HasValue ? date.Value.ToLongDateString() : "");
            var listBooking = BookingReportBLL.BookingGetByCriterion(date, cruise, CurrentUser)
                .Where(x => x.Status == StatusType.Approved)
                .List();
            //Điền guide vào lệnh điều tour
            var startRow = 3;
            var currentRow = startRow;
            var templateGuideRow = currentRow;
            var listGuideExpense = BookingReportBLL.ExpenseGetAllByCriterion(cruise.Id, date)
                .Where(x => x.Type == "Guide")
                .List(); //Lấy danh sách GuideExpense theo tàu
            var listGuide = listGuideExpense.Select(x => x.Guide).ToList();//Lấy danh sách Guide từ danh sách GuideExpense
            var listGuide_Distinct = listGuide.Distinct().ToList();//Lấy danh sách Guide không bị lặp
            sheet.InsertRow(currentRow, listGuide_Distinct.Count - 1, templateGuideRow);//Copy số row guide = với số guide
            FillGuide(listGuide_Distinct, sheet, ref currentRow);
            //--
            //Điền driver vào lệnh điều tour
            var templateDriverRow = currentRow;
            //Lấy danh sách BusByDate mà đang có booking cần export
            var listBusByDate = new List<BusByDate>();
            foreach (var booking in listBooking)
            {
                var busByDate = booking.ListBookingBusByDate.Where(x => x.BusByDate.Date == date)
                    .Select(x => x.BusByDate).ToList().FirstOrDefault();
                if (busByDate != null) { listBusByDate.Add(busByDate); }
            }
            //--
            sheet.InsertRow(currentRow, listBusByDate.Count - 1, templateDriverRow);//Copy số row driver = với số bus
            FillDriver(listBusByDate, sheet, ref currentRow);
            //--
            //Điền opt vào lệnh điều tour
            var templateOptRow = currentRow;
            //Lấy danh sách opt
            var listOpt = listGuideExpense.Select(x => x.Operator);
            var listOpt_Distinct = listOpt.Distinct().ToList();//Lấy danh sách Opt không lặp
            sheet.InsertRow(currentRow, listOpt_Distinct.Count - 1, templateOptRow);
            FillOpt(listOpt_Distinct, sheet, ref currentRow);
            //--
            //Export booking trong ngày
            int titleRow = currentRow;
            currentRow = currentRow + 2;//Chuyển current row đến templaterow booking
            int templateRow = currentRow;
            int totalRow = templateRow + listBooking.Count();
            int index = 1;
            currentRow++;//Chuyển current row đến trước template row để bắt đầu coppyrow
            sheet.InsertRow(currentRow, listBooking.Count, templateRow);
            if (CanViewSpecialRequestFood && CanViewSpecialRequestFood)
            {
                sheet.Cells[titleRow, 9, titleRow + 1, 9].Merge = true;
                sheet.Cells[titleRow, 10, titleRow + 1, 10].Merge = true;
                sheet.Cells[titleRow, 9].Value = "Special Request Food";
                sheet.Cells[titleRow, 10].Value = "Special Request Room";
            }
            else
            {
                if (CanViewSpecialRequestFood)
                {
                    sheet.Cells[titleRow, 9, titleRow + 1, 10].Merge = true;
                    sheet.Cells[titleRow, 9].Value = "Special Request Food";
                }

                if (CanViewSpecialRequestRoom)
                {
                    sheet.Cells[titleRow, 9, titleRow + 1, 10].Merge = true;
                    sheet.Cells[titleRow, 9].Value = "Special Request Room";
                }
            }
            for (int i = 0; i < listBooking.Count; i++)
            {
                var booking = listBooking[i] as Booking;
                if (booking != null)
                {
                    var name = booking.CustomerNameFull.Replace("<br/>", "\r\n").ToUpper();
                    sheet.Cells[currentRow, 1].Value = index;
                    sheet.Cells[currentRow, 2, currentRow, 3].Merge = true;
                    sheet.Cells[currentRow, 2].Value = name;
                    sheet.Cells[currentRow, 4].Value = booking.Adult;
                    sheet.Cells[currentRow, 5].Value = booking.Child;
                    sheet.Cells[currentRow, 6].Value = booking.Baby;
                    sheet.Cells[currentRow, 7].Value = booking.Trip.TripCode;
                    sheet.Cells[currentRow, 8].Value = booking.PickupAddress;
                    sheet.Cells[currentRow, 11].Value = "OS" + booking.Id;
                    sheet.Cells[currentRow, 26].Value = name;//Work around cho cột merged name không hiển thị hết khi nội dung quá dài
                    if (CanViewSpecialRequestFood && CanViewSpecialRequestFood)
                    {
                        sheet.Cells[currentRow, 9].Value = booking.SpecialRequest;
                        sheet.Cells[currentRow, 10].Value = booking.SpecialRequestRoom;
                    }
                    else
                    {
                        if (CanViewSpecialRequestFood)
                        {
                            sheet.Cells[currentRow, 9, currentRow, 10].Merge = true;
                            sheet.Cells[currentRow, 9].Value = booking.SpecialRequest;
                        }

                        if (CanViewSpecialRequestRoom)
                        {
                            sheet.Cells[currentRow, 9, currentRow, 10].Merge = true;
                            sheet.Cells[currentRow, 9].Value = booking.SpecialRequestRoom;
                        }
                    }
                    currentRow++;
                    index++;
                }
            }
            sheet.DeleteRow(templateRow);
            sheet.Cells[totalRow, 4].Value = listBooking.Sum(x => x.Adult);
            sheet.Cells[totalRow, 5].Value = listBooking.Sum(x => x.Child);
            sheet.Cells[totalRow, 6].Value = listBooking.Sum(x => x.Baby);
            //--
            //Export booking đi 2 ngày bắt đầu từ ngày hôm trước
            listBooking = BookingReportBLL.BookingGetByCriterion(date.Value.AddDays(-1), cruise, CurrentUser)
                .Where(x => x.Status == StatusType.Approved)
                .List()
                .Where(x => x.Trip.NumberOfDay > 2)
                .ToList();

            titleRow = currentRow + 2;
            templateRow = currentRow + 4;
            currentRow = templateRow + 1;
            totalRow = templateRow + listBooking.Count();
            index = 1;
            sheet.InsertRow(currentRow, listBooking.Count, currentRow - 1);
            if (CanViewSpecialRequestFood && CanViewSpecialRequestFood)
            {
                sheet.Cells[titleRow, 9, titleRow + 1, 9].Merge = true;
                sheet.Cells[titleRow, 10, titleRow + 1, 10].Merge = true;
                sheet.Cells[titleRow, 9].Value = "Special Request Food";
                sheet.Cells[titleRow, 10].Value = "Special Request Room";
            }
            else
            {
                if (CanViewSpecialRequestFood)
                {
                    sheet.Cells[titleRow, 9, titleRow + 1, 10].Merge = true;
                    sheet.Cells[titleRow, 9].Value = "Special Request Food";
                }

                if (CanViewSpecialRequestRoom)
                {
                    sheet.Cells[titleRow, 9, titleRow + 1, 10].Merge = true;
                    sheet.Cells[titleRow, 9].Value = "Special Request Room";
                }
            }
            for (int i = 0; i < listBooking.Count; i++)
            {
                var booking = listBooking[i] as Booking;
                if (booking != null)
                {
                    var name = booking.CustomerNameFull.Replace("<br/>", "\r\n").ToUpper();
                    sheet.Cells[currentRow, 1].Value = index;
                    sheet.Cells[currentRow, 2, currentRow, 3].Merge = true;
                    sheet.Cells[currentRow, 3].Value = name;
                    sheet.Cells[currentRow, 4].Value = booking.Adult;
                    sheet.Cells[currentRow, 5].Value = booking.Child;
                    sheet.Cells[currentRow, 6].Value = booking.Baby;
                    sheet.Cells[currentRow, 7].Value = booking.Trip.TripCode;
                    sheet.Cells[currentRow, 8].Value = booking.PickupAddress;
                    sheet.Cells[currentRow, 11].Value = "OS" + booking.Id;
                    sheet.Cells[currentRow, 26].Value = name;//Work around cho cột merged name không hiển thị hết khi nội dung quá dài
                    if (CanViewSpecialRequestFood && CanViewSpecialRequestFood)
                    {
                        sheet.Cells[currentRow, 9].Value = booking.SpecialRequest;
                        sheet.Cells[currentRow, 10].Value = booking.SpecialRequestRoom;
                    }
                    else
                    {
                        if (CanViewSpecialRequestFood)
                        {
                            sheet.Cells[currentRow, 9, currentRow, 10].Merge = true;
                            sheet.Cells[currentRow, 9].Value = booking.SpecialRequest;
                        }

                        if (CanViewSpecialRequestRoom)
                        {
                            sheet.Cells[currentRow, 9, currentRow, 10].Merge = true;
                            sheet.Cells[currentRow, 9].Value = booking.SpecialRequestRoom;
                        }
                    }
                    currentRow++;
                    index++;
                }
            }
            sheet.DeleteRow(templateRow);
            sheet.Cells[totalRow, 4].Value = listBooking.Sum(x => x.Adult);
            sheet.Cells[totalRow, 5].Value = listBooking.Sum(x => x.Child);
            sheet.Cells[totalRow, 6].Value = listBooking.Sum(x => x.Baby);
            //--
        }

        public void ExportTourAllVDreamCruise(ref ExcelPackage excelPackage, DateTime? date, Cruise cruise)
        {
            var sheet = excelPackage.Workbook.Worksheets["TC-VD"];
            sheet.Cells["D1"].Value = (date.HasValue ? date.Value.ToLongDateString() : "");
            var listBooking = BookingReportBLL.BookingGetByCriterion(date, cruise, CurrentUser)
                .Where(x => x.Status == StatusType.Approved)
                .List();
            if (listBooking != null && listBooking.Count > 0)
            {
                sheet.Cells["B2"].Value = listBooking[0].Trip.Name;
            }
            var startRow = 5;
            var currentRow = startRow;
            int templateRow = currentRow;
            int totalRow = templateRow + listBooking.Count();
            int index = 1;
            currentRow++;//Chuyển current row đến trước template row để bắt đầu coppyrow
            sheet.InsertRow(currentRow, listBooking.Count, templateRow);
            for (int i = 0; i < listBooking.Count; i++)
            {
                var booking = listBooking[i] as Booking;
                if (booking != null)
                {
                    var name = booking.CustomerNameFull.Replace("<br/>", "\r\n").ToUpper();
                    sheet.Cells[currentRow, 1].Value = index;
                    sheet.Cells[currentRow, 2].Value = name;
                    sheet.Cells[currentRow, 3].Value = booking.Adult;
                    sheet.Cells[currentRow, 4].Value = booking.Child;
                    sheet.Cells[currentRow, 5].Value = booking.Baby;
                    sheet.Cells[currentRow, 6].Value = booking.Trip.TripCode;
                    sheet.Cells[currentRow, 7].Value = booking.PickupAddress;
                    sheet.Cells[currentRow, 8].Value = booking.SpecialRequest;
                    sheet.Cells[currentRow, 9].Value = "OS" + booking.Id;
                    sheet.Cells[currentRow, 26].Value = name;//Work around cho cột merged name không hiển thị hết khi nội dung quá dài
                    currentRow++;
                    index++;
                }
            }
            sheet.DeleteRow(templateRow);
            sheet.Cells[totalRow, 3].Value = listBooking.Sum(x => x.Adult);
            sheet.Cells[totalRow, 4].Value = listBooking.Sum(x => x.Child);
            sheet.Cells[totalRow, 5].Value = listBooking.Sum(x => x.Baby);
        }

        [WebMethod]
        public string BookingGetAllByCriterion(string date, int cruiseId, int tripId)
        {
            var cruise = BookingReportBLL.CruiseGetById(cruiseId);
            if (!DateTime.TryParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateAsDateTime))
            {
                throw new Exception();
            }
            var bookingList = BookingReportBLL.BookingGetAllByByCriterion(CurrentUser, dateAsDateTime, cruise, new List<StatusType>() { StatusType.Approved, StatusType.Pending });// Tìm các booking theo ngày và theo tàu, nếu là all thì tìm theo tất cả các tàu
            var listBookingDTO = new List<BookingDTO>();
            foreach (var booking in bookingList)
            {
                var bookingDTO = new BookingDTO()
                {
                    Id = booking.Id,
                    BookingId = "OS" + booking.Id.ToString(),
                    CustomerName = booking.CustomerName,
                    Adult = booking.Adult,
                    Child = booking.Child,
                    Baby = booking.Baby,
                    TripName = booking.Trip?.Name,
                    CruiseName = booking.Cruise?.Name,
                    SpecialRequest = booking.SpecialRequest?.Replace(Environment.NewLine, "<br/>"),
                    SpecialRequestRoom = booking.SpecialRequestRoom?.Replace(Environment.NewLine, "<br/>"),
                    TripCode = booking.Trip.TripCode,
                    PickupAddress = booking.PickupAddress?.Replace(Environment.NewLine, "<br/>"),
                    AgencyName = booking.Agency.Name,
                    AgencyId = booking.Agency.Id,
                    SalesInChargeName = booking.Agency?.Sale?.FullName,
                    SalesInChargePhone = booking.Agency?.Sale?.Website,
                    Total = booking.Total,
                    HasInvoice = booking.HasInvoice,
                    AgencyNotes = GetAgencyNotes(booking.Agency),
                    IsWarningBooking = booking.StartDate < dateAsDateTime,
                    IsPendingBooking = booking.Status == StatusType.Pending,
                    Inspection = booking.Inspection,
                    HaveBirthdayBooking = booking.BookingRooms.Any(x => x.Customers.Any(y => y.Birthday != null && y.Birthday.Value.Day == dateAsDateTime.Day && y.Birthday.Value.Month == dateAsDateTime.Month)),
                    TripId = booking.Trip.Id,
                };
                listBookingDTO.Add(bookingDTO);
            }
            if (tripId == -1)
            {
                listBookingDTO = listBookingDTO.OrderBy(x => x.TripId).ToList();
            }
            else
            {
                listBookingDTO = listBookingDTO.Where(x => x.TripId == tripId).OrderBy(x => x.TripId).ToList();
            }
            Dispose();
            return JsonConvert.SerializeObject(listBookingDTO);
        }

        [WebMethod]
        public string ShadowBookingGetByDate(string date)
        {
            if (!DateTime.TryParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateAsDateTime))
            {
                throw new Exception();
            }
            var bookingList = BookingReportBLL.ShadowBookingGetByDate(CurrentUser, dateAsDateTime);
            var listBookingDTO = new List<BookingDTO>();
            foreach (var booking in bookingList)
            {
                var bookingDTO = new BookingDTO()
                {
                    Id = booking.Id,
                    BookingId = "OS" + booking.Id.ToString(),
                    CustomerName = booking.CustomerName,
                    Adult = booking.Adult,
                    Child = booking.Child,
                    Baby = booking.Baby,
                    TripName = booking.Trip?.Name,
                    CruiseName = booking.Cruise?.Name,
                    SpecialRequest = booking.SpecialRequest?.Replace(Environment.NewLine, "<br/>"),
                    SpecialRequestRoom = booking.SpecialRequestRoom?.Replace(Environment.NewLine, "<br/>"),
                    TripCode = booking.Trip.TripCode,
                    PickupAddress = booking.PickupAddress?.Replace(Environment.NewLine, "<br/>"),
                    AgencyName = booking.Agency.Name,
                    AgencyId = booking.Agency.Id,
                    SalesInChargeName = booking.Agency?.Sale?.FullName,
                    SalesInChargePhone = booking.Agency?.Sale?.Website,
                    Total = booking.Total,
                    HasInvoice = booking.HasInvoice,
                    AgencyNotes = GetAgencyNotes(booking.Agency),
                    IsWarningBooking = booking.StartDate < dateAsDateTime,
                    IsPendingBooking = booking.Status == StatusType.Pending,
                    Inspection = booking.Inspection,
                    HaveBirthdayBooking = booking.BookingRooms.Any(x => x.Customers.Any(y => y.Birthday != null && y.Birthday.Value.Day == dateAsDateTime.Day && y.Birthday.Value.Month == dateAsDateTime.Month)),
                    TripId = booking.Trip.Id,
                };
                var histories = BookingReportBLL.BookingHistoryGetAllByBooking(booking).OrderBy(x => x.Date).AsEnumerable();
                for (int ii = histories.Count() - 1; ii >= 0; ii--)
                {
                    if (((BookingHistory)(histories.ElementAt(ii))).StartDate == dateAsDateTime
                        || ((BookingHistory)(histories.ElementAt(ii))).Status == StatusType.Cancelled)
                    {
                        var bh = (BookingHistory)(histories.ElementAt(ii));
                        if (booking.BookingRooms.Count >= 6)
                        {
                            if (bh.Date > dateAsDateTime.AddDays(-45))
                            {
                                listBookingDTO.Add(bookingDTO);
                            }
                        }
                        else
                        {
                            if (bh.Date > dateAsDateTime.AddDays(-7))
                            {
                                listBookingDTO.Add(bookingDTO);
                            }
                        }
                        break;
                    }
                }       
            }
            Dispose();
            return JsonConvert.SerializeObject(listBookingDTO);
        }


        private string GetAgencyNotes(Agency agency)
        {
            var roles = CurrentUser.Roles;
            var note = "";
            foreach (Role role in roles)
            {
                var agencyNoteses = BookingReportBLL.AgencyNotesGetAllByAgencyAndRole(agency, role);
                note += string.Join("<br/>", agencyNoteses.Select(an => an.Note));
                if (role.Name == "Administrator")
                {
                    agencyNoteses = BookingReportBLL.AgencyNotesGetAllByAgency(agency);
                    note = string.Join("<br/>", agencyNoteses.Select(an => an.Note));
                    break;
                }
            }
            return note;
        }
    }
}
