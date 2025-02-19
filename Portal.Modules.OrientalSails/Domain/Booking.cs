using System;
using System.Collections;
using System.Collections.Generic;
using CMS.Core.Domain;
using Portal.Modules.OrientalSails.Web.Util;
using NHibernate;
using System.Linq;
using Portal.Modules.OrientalSails.Web.Admin.Enums;

namespace Portal.Modules.OrientalSails.Domain
{
    public class Booking
    {
        private IList<BookingRoom> _bookosBookingRooms;
        private StatusType _status;
        private IList<Customer> _customers;
        private IList<ExtraOption> _extraServices;
        private bool _isCharter;
        private IList _services;
        private double _transferCost;
        private DateTime? _deadline;
        private int _adult;
        private int _child;
        private int _baby;
        private bool _counted;
        private int _double;
        private int _twin;
        private bool _roomCounted;

        public virtual int Id { get; set; }
        public virtual bool Deleted { get; set; }
        public virtual User CreatedBy { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual User ModifiedBy { get; set; }
        public virtual User ConfirmedBy { get; set; }
        public virtual DateTime ModifiedDate { get; set; }
        public virtual User Partner { get; set; }
        public virtual User Sale { get; set; }
        public virtual DateTime StartDate { get; set; }
        public virtual StatusType Status
        {
            get
            {
                if (_status == StatusType.Pending && Deadline.HasValue) // nếu trạng thái là pending và có thời hạn deadline
                {
                    if (Deadline.Value < DateTime.Now) // nếu deadline nhỏ hơn ngày hiện tại
                    {
                        _status = StatusType.Cancelled;
                        IsDirty = true;
                    }
                }
                return _status;
            }
            set { _status = value; }
        }

        public virtual TripOption TripOption { get; set; }
        public virtual SailsTrip Trip { get; set; }
        public virtual double Total { get; set; }
        public virtual double Paid { get; set; }
        public virtual string Email { get; set; }
        public virtual string Name { get; set; }
        public virtual Agency Agency { get; set; }
        public virtual string PickupAddress { get; set; }
        public virtual string DropoffAddress { get; set; }
        public virtual string SpecialRequest { get; set; }
        public virtual string SpecialRequestRoom { get; set; }
        public virtual string AgencyCode { get; set; }
        public virtual double CurrencyRate { get; set; }
        public virtual double PaidBase { get; set; }
        public virtual DateTime? PaidDate { get; set; }
        public virtual bool IsPaid { get; set; }
        public virtual AccountingStatus AccountingStatus { get; set; }

        public virtual IList<Customer> Customers
        {
            get
            {
                if (_customers == null)
                {
                    _customers = new List<Customer>();
                }
                return _customers;
            }
            set { _customers = value; }
        }

        public virtual IList<BookingRoom> BookingRooms
        {
            get;
            set;
        }

        public virtual IList<ExtraOption> ExtraServices
        {
            get
            {
                if (_extraServices == null)
                {
                    _extraServices = new List<ExtraOption>();
                }
                return _extraServices;
            }
            set { _extraServices = value; }
        }

        public virtual IList Services
        {
            get
            {
                if (_services == null)
                {
                    _services = new ArrayList();
                }
                return _services;
            }
            set { _services = value; }
        }

        public virtual string BookingIdOS
        {
            get { return string.Format("OS{0:00000}", Id); }
        }
        public virtual Locked Charter { get; set; }
        public virtual double TransferCost { get; set; }
        public virtual bool IsTransferred { get; set; }
        public virtual Agency TransferTo { get; set; }
        public virtual int TransferAdult { get; set; }
        public virtual int TransferChildren { get; set; }
        public virtual int TransferBaby { get; set; }
        public virtual int Amended { get; set; }
        public virtual Cruise Cruise { get; set; }

        public virtual bool IsCharter
        {
            get
            {
                if (Charter != null)
                {
                    _isCharter = true;
                }
                return _isCharter;
            }
            set { _isCharter = value; }
        }

        //HasInvoice replace to VAT
        public virtual bool HasInvoice { get; set; }
        public virtual double CancelPay { get; set; }
        public virtual string Guide { get; set; }
        public virtual string Driver { get; set; }
        public virtual bool GuideOnboard { get; set; }
        public virtual User Locker { get; set; }
        public virtual BookingSale BookingSale { get; set; }
        public virtual bool IsDirty { get; set; }
        public virtual DateTime? Deadline
        {
            get
            {
                // Tính ngày pending max
                DateTime max;
                if ((StartDate - CreatedDate).TotalDays > 28)
                {
                    max = CreatedDate.AddDays(14);
                }
                else
                {
                    max = CreatedDate.AddDays((StartDate - CreatedDate).TotalDays); // tăng lên số ngày = 1/2 quãng thời gian
                }

                if ((StartDate - CreatedDate).TotalDays <= 3)
                {
                    // nếu tạo trước 3 ngày thì cho phép đến tận 5 giờ chiều hôm trước
                    max = StartDate.AddDays(-1).AddHours(17); // 17 giờ ngày hôm trước
                }

                if (_status == StatusType.Pending && !_deadline.HasValue)// nếu pending không giới hạn thì tự động gia hạn = max
                {
                    if (_status == StatusType.Pending)
                    {
                        _deadline = max;
                    }
                }

                //if (_deadline > max)
                //{
                //    _deadline = max;
                //}

                return _deadline;
            }
            set { _deadline = value; }
        }

        public virtual bool Special { get; set; }
        public virtual bool IsEarlyBird { get; set; }
        public virtual DateTime? LockDate { get; set; }
        public virtual User LockBy { get; set; }
        public virtual string VoucherCode { get; set; }
        public virtual VoucherBatch Batch { get; set; }
        public virtual int CutOffDays { get; set; }
        public virtual float Commission { get; set; }
        public virtual bool IsCommissionUsd { get; set; }
        public virtual string CancelledReason { get; set; }
        public virtual Series Series { get; set; }

        public virtual string LockByString
        {
            get
            {
                if (LockIncome)
                {
                    return LockBy != null ? LockBy.FullName : "system";
                }
                return "";
            }
        }

        public virtual bool LockIncome
        {
            get
            {
                if (((LockDate.HasValue && LockDate.Value < DateTime.Now) || EndDate.AddDays(1) < DateTime.Now) && LockDate != new DateTime(1992, 06, 17))
                {
                    return true;
                }
                return false;
            }
        }

        public virtual bool IsTotalUsd { get; set; }

        public virtual int Adult
        {
            get
            {
                if (Cruise.CruiseType == Web.Admin.Enums.CruiseType.Cabin)
                {
                    return BookingRooms.Sum(x => x.Adult);
                }
                else if (Cruise.CruiseType == Web.Admin.Enums.CruiseType.Seating)
                {
                    return Customers.Where(x => x.Type == CustomerType.Adult).Count();
                }
                return 0;
            }
        }

        public virtual int Pax
        {
            get
            {
                if (Cruise.CruiseType == Web.Admin.Enums.CruiseType.Cabin)
                {
                    return Adult + Child;
                }
                else if (Cruise.CruiseType == Web.Admin.Enums.CruiseType.Seating)
                {
                    return Adult + Child + Baby;
                }
                return 0;
            }
        }

        public virtual int Child
        {
            get
            {
                if (Cruise.CruiseType == Web.Admin.Enums.CruiseType.Cabin)
                {
                    return BookingRooms.Sum(x => x.Child);
                }
                else if (Cruise.CruiseType == Web.Admin.Enums.CruiseType.Seating)
                {
                    return Customers.Where(x => x.Type == CustomerType.Children).Count();
                }
                return 0;
            }
        }

        public virtual int Baby
        {
            get
            {
                if (Cruise.CruiseType == Web.Admin.Enums.CruiseType.Cabin)
                {
                    return BookingRooms.Sum(x => x.Baby);
                }
                else if (Cruise.CruiseType == Web.Admin.Enums.CruiseType.Seating)
                {
                    return Customers.Where(x => x.Type == CustomerType.Baby).Count();
                }
                return 0;
            }
        }

        protected virtual void GetChild()
        {
            _child = 0;
            _baby = 0;
            foreach (BookingRoom room in BookingRooms)
            {
                if (room.HasBaby)
                {
                    _baby++;
                }
                if (room.HasChild)
                {
                    _child++;
                }
            }
            _counted = true;
        }

        public virtual int DoubleCabin
        {
            get
            {
                if (!_roomCounted)
                {
                    CountCabin();
                }
                return _double;
            }
        }

        public virtual int TwinCabin
        {
            get
            {
                if (!_roomCounted)
                {
                    CountCabin();
                }
                return _twin;
            }
        }

        protected virtual void CountCabin()
        {
            _double = 0;
            _twin = 0;
            foreach (BookingRoom bookingRoom in BookingRooms)
            {
                if (bookingRoom.RoomType != null && bookingRoom.RoomType.Id == SailsModule.DOUBLE)
                {
                    _double++;
                }
                if (bookingRoom.RoomType != null && bookingRoom.RoomType.IsShared)
                {
                    _twin += bookingRoom.Adult;
                }
            }
            _roomCounted = true;
        }

        public virtual double MoneyLeft
        {
            get
            {
                // Nếu đã trả rồi thì là 0
                if (IsPaid)
                {
                    return 0;
                }
                // Nếu không tính bằng (tổng giá in USD - đã trả in USD)* tỉ giá - đã trã in VNĐ == còn lại theo VNĐ
                //return (Value - Paid) * CurrencyRate - PaidBase;
                if (IsTotalUsd)
                {
                    var agencyReceivable = Total - (GuideCollect) - Paid;
                    if (PaidBase > 0)
                    {
                        agencyReceivable = agencyReceivable - (PaidBase / CurrencyRate);
                    }
                    return agencyReceivable;
                }
                else
                {
                    return Total - PaidBase - GuideCollect - (Paid * CurrencyRate);
                }
            }
        }

        public virtual string ContactEmail
        {
            get
            {
                if (string.IsNullOrEmpty(Email))
                {
                    if (CreatedBy != null)
                    {
                        return CreatedBy.Email;
                    }
                    return string.Empty;
                }
                return Email;
            }
        }

        public virtual string ContactName
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                {
                    if (CreatedBy != null)
                    {
                        return CreatedBy.FullName;
                    }
                    return string.Empty;
                }
                return Name;
            }
        }

        public virtual string CustomerName
        {
            get
            {
                string name = string.Empty;
                int number = 0;
                if (Cruise.CruiseType != CruiseType.Seating)
                {
                    foreach (BookingRoom room in BookingRooms)
                    {
                        foreach (Customer customer in room.RealCustomers)
                        {
                            if (!string.IsNullOrEmpty(customer.Fullname))
                            {
                                if (number >= 2)
                                    break;
                                name = name + customer.Fullname + "<br/>";
                                number++;
                            }
                        }
                    }
                }
                else
                {
                    foreach (Customer customer in Customers)
                    {
                        if (!string.IsNullOrEmpty(customer.Fullname))
                        {
                            if (number >= 2)
                                break;
                            name = name + customer.Fullname + "<br/>";
                            number++;
                        }
                    }
                }

                return name;
            }
        }

        public virtual string CustomerNameFull
        {
            get
            {
                string name = string.Empty;
                int number = 0;
                if (Cruise.CruiseType == Web.Admin.Enums.CruiseType.Cabin)
                {
                    foreach (BookingRoom room in BookingRooms)
                    {
                        foreach (Customer customer in room.RealCustomers)
                        {
                            if (!string.IsNullOrEmpty(customer.Fullname))
                            {
                                name = name + customer.Fullname + "<br/>";
                            }
                        }
                    }
                }
                else if (Cruise.CruiseType == Web.Admin.Enums.CruiseType.Seating)
                {
                    foreach (Customer customer in Customers)
                    {
                        if (!string.IsNullOrEmpty(customer.Fullname))
                        {
                            name = name + customer.Fullname + "<br/>";
                        }
                    }
                }
                return name;
            }
        }

        public virtual string RoomName
        {
            get
            {
                string name = string.Empty;
                try
                {
                    Dictionary<string, int> rooms = new Dictionary<string, int>();
                    foreach (BookingRoom room in BookingRooms)
                    {
                        string key = "";
                        if (room.Room != null)
                        {
                            key = room.Room.RoomClass.Name + " " + room.Room.RoomType.Name;
                        }
                        else
                        {
                            key = room.RoomClass.Name + " " + room.RoomType.Name;
                        }
                        if (rooms.ContainsKey(key))
                        {
                            rooms[key] += 1;
                        }
                        else
                        {
                            rooms.Add(key, 1);
                        }
                    }

                    foreach (KeyValuePair<string, int> entry in rooms)
                    {
                        name += entry.Value + " " + entry.Key + "<br/>";
                    }
                }
                catch
                {
                }
                return name;
            }
        }

        public virtual string Confirmer
        {
            get
            {
                if (ConfirmedBy != null)
                {
                    return ConfirmedBy.FullName;
                }
                return "System";
            }
        }

        public virtual string Note { get; set; }
        public virtual AgencyContact Booker { get; set; }
        public virtual bool IsApproved { get; set; }
        public virtual bool IsPaymentNeeded { get; set; }
        public virtual double Value
        {
            get
            {
                switch (_status)
                {
                    case StatusType.Approved:
                        return Total;
                    case StatusType.Cancelled:
                        return CancelPay;
                    default:
                        return 0;
                }
            }
        }

        public virtual double TotalReceivable
        {
            get { return GuideCollectReceivable + AgencyReceivable; }
        }

        public virtual double AgencyReceivable
        {
            get
            {
                // Nếu đã trả rồi thì là 0
                if (IsPaid)
                {
                    return 0;
                }
                // Nếu không tính bằng (tổng giá in USD - đã trả in USD - guide collect)* tỉ giá - đã trã in VNĐ == còn lại theo VNĐ
                // 
                //return (Value - Paid - GuideCollect) * CurrencyRate - PaidBase;
                return MoneyLeft;
            }
        }

        public virtual bool Inspection { set; get; }
        public virtual DateTime EndDate { get; set; }

        public virtual double Calculate(SailsModule Module, Agency agency, double childPrice, double agencySup, bool customPrice, bool singleService)
        {
            Role applyRole;
            if (agency != null && agency.Role != null)
            {
                applyRole = agency.Role;
            }
            else
            {
                applyRole = Module.RoleGetById(4);
            }
            double total = 0;

            Role role;
            if (Agency != null && Agency.Role != null)
            {
                role = Agency.Role;
            }
            else
            {
                role = applyRole;
            }
            IList _policies = Module.AgencyPolicyGetByRole(role);

            #region -- Giá dịch vụ --

            IList services = Module.ExtraOptionGetBooking();
            IList servicePrices = Module.ServicePriceGetByBooking(this);

            foreach (ExtraOption extra in services)
            {
                double child = Child;
                double unitPrice = -1;
                foreach (BookingServicePrice price in servicePrices)
                {
                    if (price.ExtraOption == extra)
                    {
                        unitPrice = price.UnitPrice;
                    }
                }
                if (unitPrice < 0)
                {
                    unitPrice = Module.ApplyPriceFor(extra.Price, _policies);
                }

                if (extra.IsIncluded)
                {
                    // Nếu dịch vụ đã include thì xem xem có không check không để trừ
                    if (!_extraServices.Contains(extra))
                    {
                        total -= unitPrice * (Adult + child * childPrice / 100);
                    }
                }
                else
                {
                    // Nếu là dịch vụ chưa include thì xem có có không để cộng
                    if (_extraServices.Contains(extra))
                    {
                        total += unitPrice * (Adult + child * childPrice / 100);
                    }
                }
            }

            //TODO: Cần phải check cả dịch vụ dành cho từng khách nữa

            ////foreach (ExtraOption extra in _extraServices)
            ////{
            ////    if (services.Contains(extra))
            ////    {
            ////        total += Module.ApplyPriceFor(extra.Price, _policies)*Adult;
            ////    }
            ////}
            #endregion

            #region -- giá phòng --
            // Tính giá theo từng phòng
            IList cServices = Module.ExtraOptionGetCustomer();
            foreach (BookingRoom broom in BookingRooms)
            {
                if (customPrice)
                {
                    double tempTotal = 0;
                    foreach (Customer customer in broom.RealCustomers)
                    {
                        tempTotal += customer.Total;
                    }
                    if (tempTotal > 0)
                    {
                        total += tempTotal;
                    }
                    else
                    {
                        total += broom.Total;
                    }
                }
                else
                {
                    total += broom.Calculate(Module, _policies, childPrice, agencySup, Agency);
                }

                if (singleService)
                {
                    // Ngoài giá phòng còn có thể có dịch vụ cá nhân
                    foreach (Customer customer in broom.RealCustomers)
                    {
                        // Baby không tính dịch vụ
                        if (customer.Type == CustomerType.Baby)
                        {
                            continue;
                        }

                        foreach (ExtraOption service in cServices)
                        {
                            CustomerService customerService = Module.CustomerServiceGetByCustomerAndService(customer,
                                                                                                            service);
                            if (customerService != null)
                            {
                                double rate = 1;
                                //if (customer.IsChild)
                                //{
                                //    rate = childPrice / 100;
                                //}

                                double unitPrice = -1;
                                foreach (BookingServicePrice price in servicePrices)
                                {
                                    if (price.ExtraOption == service)
                                    {
                                        unitPrice = price.UnitPrice;
                                    }
                                }
                                if (unitPrice < 0)
                                {
                                    unitPrice = Module.ApplyPriceFor(service.Price, _policies);
                                }

                                if (service.IsIncluded && customerService.IsExcluded)
                                {
                                    // Nếu dịch vụ có included mà lại bị excluded thì trừ
                                    total -= unitPrice * rate;
                                }

                                if (!service.IsIncluded && !customerService.IsExcluded)
                                {
                                    // Nếu dịch vụ không included mà lại có thì cộng
                                    total += unitPrice * rate;
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            return total;
        }

        public virtual double Calculate(SailsModule Module, Agency agency, double childPrice, double agencySup, bool customPrice, double roomTotal)
        {
            Role applyRole;
            if (agency != null && agency.Role != null)
            {
                applyRole = agency.Role;
            }
            else
            {
                applyRole = Module.RoleGetById(4);
            }
            double total = roomTotal;
            #region -- Lấy danh sách chính sách giá --
            Role role;
            if (Agency != null && Agency.Role != null)
            {
                role = Agency.Role;
            }
            else
            {
                role = applyRole;
            }
            IList _policies = Module.AgencyPolicyGetByRole(role);
            #endregion

            #region -- Giá dịch vụ --

            IList services = Module.ExtraOptionGetBooking();
            IList servicePrices = Module.ServicePriceGetByBooking(this);

            foreach (ExtraOption extra in services)
            {
                double child = Child;

                double unitPrice = -1;
                // Với mỗi dịch vụ ưu tiên lấy giá nhập trước
                foreach (BookingServicePrice price in servicePrices)
                {
                    if (price.ExtraOption == extra)
                    {
                        unitPrice = price.UnitPrice;
                    }
                }

                if (unitPrice < 0)
                {
                    unitPrice = Module.ApplyPriceFor(extra.Price, _policies);
                }

                if (extra.IsIncluded)
                {
                    // Nếu dịch vụ đã include thì xem xem có không check không để trừ
                    if (!_extraServices.Contains(extra))
                    {
                        total -= unitPrice * (Adult + child * childPrice / 100);
                    }
                }
                else
                {
                    // Nếu là dịch vụ chưa include thì xem có có không để cộng
                    if (_extraServices.Contains(extra))
                    {
                        total += unitPrice * (Adult + child * childPrice / 100);
                    }
                }
            }

            //TODO: Cần phải check cả dịch vụ dành cho từng khách nữa

            #endregion

            return total;
        }

        public virtual Dictionary<CostType, double> Cost(CostingTable table, IList costTypes)
        {
            Dictionary<CostType, double> serviceTotal = new Dictionary<CostType, double>();
            foreach (CostType type in costTypes)
            {
                serviceTotal.Add(type, 0);
            }

            // Lấy bảng giá theo dịch vụ
            Dictionary<CostType, Costing> map = table.GetCostMap(costTypes);
            foreach (CostType type in costTypes)
            {
                // Tính lại giá với các dịch vụ cố định, bắt buộc tính (không phải dịch vụ do khách lựa chọn)
                if (!type.IsDailyInput && type.Service == null)
                {
                    if (map.ContainsKey(type))
                    {
                        // Giá dịch vụ = tổng số child, tổng số baby, tổng số adult
                        serviceTotal[type] = map[type].Adult * Adult + map[type].Child * Child + map[type].Baby * Baby;
                    }
                }

                if (!type.IsDailyInput && type.Service != null) // Nếu không nhập thủ công, và có dịch vụ đi kèm
                {
                    if (!map.ContainsKey(type))
                    {
                        throw new Exception("Price setting is not completed:" + type.Name);
                    }
                    // Nếu là giá tương ứng với dịch vụ thì phải xem lại
                    // Nếu là dịch vụ cho từng khách
                    if (type.Service.Target == ServiceTarget.Customer)
                    {
                        foreach (BookingRoom bkroom in BookingRooms)
                        {
                            // Workaround: nếu là khách thứ hai và là single thì bỏ qua
                            int _index = 0;
                            foreach (Customer customer in bkroom.Customers)
                            {
                                _index++;
                                if (bkroom.IsSingle && _index == 2)
                                {
                                    continue;
                                }
                                if (customer.CustomerExtraOptions.Contains(type.Service))
                                {
                                    if (customer.IsChild)
                                    {
                                        serviceTotal[type] += map[type].Child;
                                        continue;
                                    }
                                    switch (customer.Type)
                                    {
                                        case CustomerType.Adult:
                                            serviceTotal[type] += map[type].Adult;
                                            break;
                                        case CustomerType.Children:
                                            serviceTotal[type] += map[type].Child;
                                            break;
                                        case CustomerType.Baby:
                                            serviceTotal[type] += map[type].Baby;
                                            break;
                                    }
                                }
                            }
                        }
                    }

                    // Nếu là dịch vụ cho cả book
                    if (type.Service.Target == ServiceTarget.Booking)
                    {
                        serviceTotal[type] = map[type].Adult * Adult + map[type].Child * Child + map[type].Baby * Baby;
                    }
                }
            }

            return serviceTotal;
        }

        public virtual bool GuideConfirmed { get; set; }
        public virtual bool AgencyConfirmed { get; set; }
        public virtual double GuideCollect { get; set; }
        public virtual bool GuideCollected { get; set; }
        public virtual double GuideCollectedUSD { get; set; }
        public virtual double GuideCollectedVND { get; set; }

        public virtual double GuideCollectReceivable
        {
            get
            {
                // Nếu đã trả rồi thì là 0
                if (GuideCollected)
                {
                    return 0;
                }
                // Nếu không tính bằng (tổng giá in USD - đã trả in USD)* tỉ giá - đã trã in VNĐ == còn lại theo VNĐ
                // 
                return (GuideCollect - GuideCollectedUSD) * CurrencyRate - GuideCollectedVND;
            }
        }

        private IList<Transaction> _transactions;
        public virtual IList<Transaction> Transactions
        {
            get
            {
                if (_transactions == null)
                {
                    _transactions = new List<Transaction>();
                }
                return _transactions;
            }
            set { _transactions = value; }
        }

        private int _group;
        public virtual int Group
        {
            get
            {
                if (_group == 0)
                {
                    _group = 1;
                }
                return _group;
            }
            set { _group = value; }
        }
        public virtual BusType Transfer_BusType { get; set; }
        public virtual string Transfer_Service { get; set; }
        public virtual DateTime? Transfer_DateTo { get; set; }
        public virtual DateTime? Transfer_DateBack { get; set; }
        public virtual string Transfer_Note { get; set; }
        public virtual string PickupTime { get; set; }
        public virtual IList<BookingBusByDate> ListBookingBusByDate { get; set; }
        public virtual bool Transfer_Upgraded { get; set; }
        public virtual ICollection<BookingHistory> BookingHistories { get; set; }
        public virtual bool CO_AN_CHAY { get; set; }
        public virtual TransferLocationType DIA_DIEM_TRANSFER { get; set; }
        public virtual ICollection<BookingRoomPrice> BookingRoomPrices { get; set; }
        public virtual int RoomCount { get; set; }
        public virtual int CustomerCount { get; set; }

        public virtual IList<RoomPriceSalesInput> RoomPriceSalesInputs
        {
            get
            {
                var roomClassTypeGroups = BookingRooms.GroupBy(x => new { x.Room.RoomClass, x.Room.RoomType });
                var roomPrices = new List<RoomPriceSalesInput>();
                foreach (var roomClassTypeGroup in roomClassTypeGroups)
                {
                    var roomPrice = new RoomPriceSalesInput()
                    {
                        RoomClassId = roomClassTypeGroup.Key.RoomClass.Id,
                        RoomClassName = roomClassTypeGroup.Key.RoomClass.Name,
                        RoomTypeId = roomClassTypeGroup.Key.RoomType.Id,
                        RoomTypeName = roomClassTypeGroup.Key.RoomType.Name,
                        NumberOfRooms = roomClassTypeGroup.Where(x => x.IsSingle == false).Count(),
                        NumberOfRoomsSingle = roomClassTypeGroup.Where(x => x.IsSingle == true).Count(),
                        NumberOfAddAdult = roomClassTypeGroup.Where(x => x.HasAddAdult).Count(),
                        NumberOfAddBaby = roomClassTypeGroup.Where(x => x.HasBaby).Count(),
                        NumberOfAddChild = roomClassTypeGroup.Where(x => x.HasChild).Count(),
                        NumberOfExtrabed = roomClassTypeGroup.Where(x => x.HasAddExtraBed).Count(),
                    };

                    roomPrices.Add(roomPrice);
                }

                return roomPrices;
            }
        }
    }

    public enum AccountingStatus
    {
        New,
        Modified,
        Updated
    }

    public class RoomPriceSalesInput
    {
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; }
        public int RoomClassId { get; set; }
        public string RoomClassName { get; set; }
        public int NumberOfRooms { get; set; }
        public int NumberOfAddAdult { get; set; }
        public int NumberOfAddChild { get; set; }
        public int NumberOfAddBaby { get; set; }
        public int NumberOfExtrabed { get; set; }
        public int NumberOfRoomsSingle { get; set; }
    }
}
