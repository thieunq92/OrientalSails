
namespace Portal.Modules.OrientalSails
{
    public class Permission
    {
        #region -- Quyền xuất dữ liệu --
        /// <summary>
        /// Quyền xuất bảng công nợ
        /// </summary>
        public const string ACTION_EXPORTCONGNO = "ACTION_EXPORTCONGNO";

        /// <summary>
        /// Quyền xuất doanh thu không có agency
        /// </summary>
        public const string ACTION_EXPORTSELFSALES = "ACTION_EXPORTSELFSALES";

        /// <summary>
        /// Quyền xuất doanh thu (toàn bộ) (
        /// </summary>
        public const string ACTION_EXPORTREVENUE = "ACTION_EXPORTREVENUE";

        /// <summary>
        /// Quyền xuất doanh thu lọc theo sales (1 sale = 1 sheet)
        /// </summary>
        public const string ACTION_EXPORTREVENUEBYSALE = "ACTION_EXPORTREVENUEBYSALE";

        /// <summary>
        /// Quyền xuất danh sách đại lý
        /// </summary>
        public const string ACTION_EXPORTAGENCY = "ACTION_EXPORTAGENCY";
        #endregion

        #region -- quyền view form --

        public const string FORM_ADDBOOKING = "FORM_ADDBOOKING";
        public const string FORM_AGENCYEDIT = "FORM_AGENCYEDIT";
        public const string FORM_AGENCYLIST = "FORM_AGENCYLIST";
        public const string FORM_AGENTLIST = "FORM_AGENTLIST";
        public const string FORM_BALANCEREPORT = "FORM_BALANCEREPORT";
        public const string FORM_BOOKINGLIST = "FORM_BOOKINGLIST";
        public const string FORM_BOOKINGREPORT = "FORM_BOOKINGREPORT";
        public const string FORM_BOOKINGREPORTPERIODALL = "FORM_BOOKINGREPORTPERIODALL";
        //public const string FORM_BOOKINGREPORTRERIOD = "FORM_BOOKINGREPORTPERIOD";
        public const string FORM_BOOKINGREPORTRERIOD = "FORM_BOOKINGREPORTPERIOD";
        
        public const string FORM_COSTING = "FORM_COSTING";
        public const string FORM_COSTTYPES = "FORM_COSTTYPES";
        public const string FORM_CRUISECONFIG = "FORM_CRUISECONFIG";
        public const string FORM_CRUISESEDIT = "FORM_CRUISESEDIT";
        public const string FORM_CRUISESLIST = "FORM_CRUISESLIST";
        public const string FORM_CUSTOMERCOMMENT = "FORM_CUSTOMERCOMMENT";
        public const string FORM_EXCHANGERATE = "FORM_EXCHANGERATE";
        public const string FORM_EXPENSEREPORT = "FORM_EXPENSEREPORT";
        public const string FORM_EXTRAOPTIONEDIT = "FORM_EXTRAOPTIONEDIT";
        public const string FORM_INCOMEREPORT = "FORM_INCOMEREPORT";
        public const string FORM_ORDERREPORT = "FORM_ORDERREPORT";
        public const string FORM_PAYABLELIST = "FORM_PAYABLELIST";
        public const string FORM_PAYMENTREPORT = "FORM_PAYMENTREPORT";
        public const string FORM_ROOMCLASSEDIT = "FORM_ROOMCLASSEDIT";
        public const string FORM_ROOMEDIT = "FORM_ROOMEDIT";
        public const string FORM_ROOMLIST = "FORM_ROOMLIST";
        public const string FORM_ROOMTYPEXEDIT = "FORM_ROOMTYPEXEDIT";
        public const string FORM_SAILSTRIPEDIT = "FORM_SAILSTRIPEDIT";
        public const string FORM_SAILSTRIPLIST = "FORM_SAILSTRIPLIST";
        public const string FORM_TRACKINGREPORT = "FORM_TRACKINGREPORT";
        public const string FORM_BOOKINGPAYMENT = "FORM_BOOKINGPAYMENT";
        public const string FORM_RECEIVABLETOTAL = "FORM_RECEIVABLETOTAL";
        public const string FORM_EXPENSEPERIOD = "FORM_EXPENSEPERIOD";
        #endregion

        #region -- quyền thao tác dữ liệu --
        public const string ACTION_EDITBOOKING = "ACTION_EDITBOOKING";
        public const string ACTION_EDITAGENCY = "ACTION_EDITAGENCY";

        public const string ACTION_LOCKINCOME = "LOCK_INCOME";
        public const string ACTION_EDIT_AFTER_LOCK = "EDIT_AFTER_LOCK";
        public const string ACTION_EDIT_TOTAL = "EDIT_TOTAL";
        public const string ACTION_EDIT_TOTAL_DETAILS = "EDIT_TOTAL_DETAILS";

        public const string ACTION_EDIT_TRIP_AFTER = "EDIT_TRIP_AFTER";

        public const string EDIT_SALE_IN_CHARGE = "EDIT_SALE_IN_CHARGE";
        public const string VIEW_TOTAL_BY_DATE = "VIEW_TOTAL_BY_DATE";
        public const string VIEW_ALL_AGENCY = "VIEW_ALL_AGENCY";

        #endregion

    }
}
