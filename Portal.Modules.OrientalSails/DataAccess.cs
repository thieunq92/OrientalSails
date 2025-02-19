using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using CMS.Core.Domain;
using iTextSharp.text;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using NHibernate.Type;
using Org.BouncyCastle.Asn1.IsisMtt.X509;
using Portal.Modules.OrientalSails.Domain;
using Portal.Modules.OrientalSails.Web.Admin;
using Portal.Modules.OrientalSails.Web.Util;
using System.Reflection;
using System.Web.Configuration;
using Castle.Core;
using Costing = Portal.Modules.OrientalSails.Domain.Costing;
using SailsPriceConfig = Portal.Modules.OrientalSails.Domain.SailsPriceConfig;
using CMS.Core.DataAccess;
using CMS.Core.Util;
using NHibernate.Dialect.Function;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Portal.Modules.OrientalSails.Utils;

namespace Portal.Modules.OrientalSails
{
    public partial class SailsModule
    {
        public T GetById<T>(int id)
        {
            return (T)_commonDao.GetObjectById(typeof(T), id);
        }
        #region -- Room Type --
        public ICommonDao CommonDao
        {
            get
            {
                return _commonDao;
            }
        }

        public void Save(RoomTypex roomTypex)
        {
            _commonDao.SaveObject(roomTypex);
        }

        public void Update(RoomTypex roomTypex)
        {
            _commonDao.UpdateObject(roomTypex);
        }

        public RoomTypex RoomTypexGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(RoomTypex), id) as RoomTypex;
        }

        public IList RoomTypexGetAll()
        {
            return _sailsDao.RoomTypexGetAll();
        }

        public IList RoomTypeGetByCruise(Cruise cruise)
        {
            return _sailsDao.RoomTypeGetByCruise(cruise);
        }

        public int RoomTypeCount()
        {
            return _commonDao.Count(typeof(RoomTypex));
        }

        #endregion

        #region -- Room Class --

        public void Save(RoomClass roomClass)
        {
            _commonDao.SaveObject(roomClass);
        }

        public void Update(RoomClass roomClass)
        {
            _commonDao.UpdateObject(roomClass);
        }

        public RoomClass RoomClassGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(RoomClass), id) as RoomClass;
        }

        public IList RoomClassGetAll()
        {
            return _sailsDao.RoomClassGetAll();
        }

        public IList RoomClassGetByCruise(Cruise cruise)
        {
            if (cruise != null)
            {
                return _commonDao.GetObjectByCriterion(typeof(RoomClass), Restrictions.Eq("Cruise", cruise));
            }
            return _commonDao.GetAll(typeof(RoomClass));
        }

        public int RooomClassCount()
        {
            return _commonDao.Count(typeof(RoomClass));
        }

        #endregion

        #region -- Room --

        public void Save(Room room)
        {
            _commonDao.SaveObject(room);
        }

        public void Update(Room room)
        {
            _commonDao.UpdateObject(room);
        }

        public void Delete(Room room)
        {
            room.Deleted = true;
            _commonDao.SaveOrUpdateObject(room);
        }

        public Room RoomGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(Room), id) as Room;
        }

        public int RoomCount()
        {
            return _commonDao.Count(typeof(Room));
        }

        /// <summary>
        /// Đếm số phòng thuộc một loại trong một danh sách phòng cho trước
        /// </summary>
        /// <param name="roomClass">Đẳng cấp</param>
        /// <param name="roomTypex">Loại giường</param>
        /// <param name="listRoom">Danh sách phòng</param>
        /// <returns></returns>
        public int RoomCount(RoomClass roomClass, RoomTypex roomTypex, IList listRoom)
        {
            int count = 0;
            foreach (Room room in listRoom)
            {
                if (room.RoomClass == roomClass && room.RoomType == roomTypex && room.IsAvailable)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Lấy số phòng còn trống theo một ngày cho trước, số chỗ trống đối với phòng shared trên một tàu xác định
        /// </summary>
        /// <param name="roomClass">Đẳng cấp phòng</param>
        /// <param name="roomType">Loại phòng</param>
        /// <param name="cruise">Tàu cần kiểm tra</param>
        /// <param name="date">Ngày đặt phòng</param>
        /// <param name="duration">Số ngày trống liên tiếp</param>
        /// <param name="passLock">Bỏ qua phòng khóa</param>
        /// <returns>Số phòng, chỗ trống, -1 nếu không tồn tại phòng</returns>
        public int RoomCount(RoomClass roomClass, RoomTypex roomType, Cruise cruise, DateTime date, int duration,
                             bool passLock, int halfDay)
        {
            // Điều kiện là cùng một loại kiểu phòng và trên cùng tàu
            ICriterion sametype = Restrictions.And(Restrictions.Eq("RoomClass", roomClass),
                                                 Restrictions.Eq("RoomType", roomType));
            sametype = Restrictions.And(sametype, Restrictions.Eq("Cruise", cruise));
            sametype = Restrictions.And(Restrictions.Eq("Deleted", false), sametype);

            // Tổng số phòng trên tàu
            int all = _commonDao.CountObjectByCriterion(typeof(Room), sametype);

            // Nếu không có phòng nào, trả về -1
            if (all == 0) return -1;

            // Nếu không bỏ qua lock và là ngày đã có khóa
            //TODO: Trên thực tế nếu đã bỏ qua khóa thì bỏ qua tất cả các điều kiện khác
            if (!passLock && LockedCheckByDate(cruise, date, date.AddDays(duration - 1)) != null)
            {
                return 0;
            }

            int book = 0;

            IList booked = _sailsDao.BookingRoomNotAvaiable(cruise, date, duration);

            int onboard = 0;

            #region -- Lấy số phòng tối đa đã có người trong hành trình --

            // Tính số phòng đã có người theo từng ngày một nếu là hành trình nhiều ngày
            if (duration > 1)
            {
                for (int ii = 0; ii < duration - 1; ii++)
                {
                    DateTime today = date.AddDays(ii);
                    foreach (BookingRoom room in booked)
                    {
                        if (room.Room != null && room.Room.RoomClass.Id == roomClass.Id && room.Room.RoomType.Id == roomType.Id)
                        {
                            // Nếu là ngày đầu tiên, các booking mới trong ngày hoặc từ trước đều tính (Vì đã lọc chỉ lấy các booking vướng theo
                            // đó đã có điều kiện booking chưa kết thúc. Không có check book kết thúc cho ngày đầu tiên
                            if (ii == 0)
                            {
                                if (room.Book.StartDate <= today)
                                {
                                    onboard = AddBookCount(roomType, room, onboard, false); // Tăng thêm onboard
                                }
                            }
                            else
                            {
                                if (room.Book.StartDate == today)
                                {
                                    onboard = AddBookCount(roomType, room, onboard, false);
                                    // Nếu là người lên tàu thì thêm vào
                                }
                                if (room.Book.EndDate == today)
                                {
                                    onboard = AddBookCount(roomType, room, onboard, true);
                                    // Nếu là người xuống tàu thì trừ đi
                                }
                            }
                        }
                    }

                    book = Math.Max(book, onboard); // Số phòng vướng sẽ là max của số phòng bị book từng ngày
                }
            }
            else
            {
                // Nếu số ngày là 1
                // Lấy cả ngày và nửa ngày tương ứng với nó

                foreach (BookingRoom room in booked)
                {
                    if (room.Room != null && room.RoomClass.Id == roomClass.Id && room.RoomType.Id == roomType.Id)
                    {
                        // Nếu là ngày đầu tiên, các booking mới trong ngày hoặc từ trước đều tính (Vì đã lọc chỉ lấy các booking vướng theo
                        // đó đã có điều kiện booking chưa kết thúc. Không có check book kết thúc cho ngày đầu tiên

                        if (room.Book.StartDate <= date)
                        {
                            //TODO: thêm buổi
                            if (room.Book.Trip.NumberOfDay > 1 || room.Book.Trip.HalfDay == 0 || room.Book.Trip.HalfDay == halfDay)
                                onboard = AddBookCount(roomType, room, onboard, false); // Tăng thêm onboard
                        }

                    }
                }

                book = Math.Max(book, onboard);
            }


            #endregion

            // Nếu là phòng ở ghép phải tính theo số chỗ

            if (roomType.IsShared)
            {
                return (all * roomType.Capacity - book);
            }
            return all - book;
        }

        /// <summary>
        /// Lấy số phòng còn trống theo một ngày cho trước, số chỗ trống đối với phòng shared
        /// </summary>
        /// <param name="roomClass">Đẳng cấp phòng</param>
        /// <param name="roomType">Loại phòng</param>
        /// <param name="cruise">Tàu cần kiểm tra</param>
        /// <param name="date">Ngày đặt phòng</param>
        /// <param name="duration">Số ngày trống liên tiếp</param>
        /// <returns>Số phòng, chỗ trống, -1 nếu không tồn tại phòng</returns>
        public int RoomCount(RoomClass roomClass, RoomTypex roomType, Cruise cruise, DateTime date, int duration, int halfday)
        {
            return RoomCount(roomClass, roomType, cruise, date, duration, false, halfday);
        }

        public int AvaiableSeatCount(Cruise cruise, DateTime date, SailsTrip trip)
        {
            ICriterion cruiseCriterion = Restrictions.Eq("Id", cruise.Id);
            var cruises = _commonDao.GetObjectByCriterion(typeof(Cruise), cruiseCriterion);

            if (cruises.Count > 0)
            {
                var foundedCruise = cruises[0] as Cruise;
                var seatNumberOfCruise = foundedCruise.NumberOfSeat;
                ICriterion usedSeatCriterion = Restrictions.And(Restrictions.Eq("Cruise", cruise), Restrictions.Eq("Trip", trip));
                usedSeatCriterion = Restrictions.And(usedSeatCriterion, Restrictions.Le("StartDate", date));
                usedSeatCriterion = Restrictions.And(usedSeatCriterion, Restrictions.Ge("EndDate", date));
                usedSeatCriterion = Restrictions.And(usedSeatCriterion, Restrictions.Eq("Trip", trip));
                usedSeatCriterion = Restrictions.And(usedSeatCriterion, Restrictions.Not(Restrictions.Eq("Status", StatusType.Cancelled)));
                var bookingsUsedSeat = _commonDao.GetObjectByCriterion(typeof(Booking), usedSeatCriterion);
                var usedSeat = 0;
                foreach (Booking booking in bookingsUsedSeat)
                {
                    usedSeat += booking.Adult + booking.Child + booking.Baby;
                }
                return seatNumberOfCruise - usedSeat;
            }
            else
            {
                throw new Exception("Not found cruise");
            }
        }

        /// <summary>
        /// Cộng thêm số phòng đã book vào
        /// </summary>
        /// <param name="roomType">Kiểu phòng</param>
        /// <param name="room">Thông tin book phòng</param>
        /// <param name="book">Số lượng hiện tại</param>
        /// <param name="minus">Có phải là trừ thay vì cộng hay không</param>
        /// <returns></returns>
        private static int AddBookCount(RoomTypex roomType, BookingRoom room, int book, bool minus)
        {
            // Nếu là phòng shared thì tính theo số khách
            if (roomType.IsShared)
            {
                if (minus)
                {
                    book -= room.VirtualAdult; // Tính dựa theo số khách ảo để loại ra các phòng book single
                }
                else
                {
                    book += room.VirtualAdult; // Tính dựa theo số khách ảo để loại ra các phòng book single
                }
            }
            else
            {
                if (minus)
                {
                    book--;
                }
                else
                {
                    book++;
                }
            }
            return book;
        }

        /// <summary>
        /// Lấy số phòng còn trống theo một ngày cho trước, số chỗ trống đối với phòng shared twin, không tính tới booking này
        /// </summary>
        /// <param name="roomClass">Đẳng cấp phòng</param>
        /// <param name="roomType">Loại phòng</param>
        /// <param name="cruise"></param>
        /// <param name="date">Ngày đặt phòng</param>
        /// <param name="duration">Số ngày trống liên tiếp</param>
        /// <param name="exception">Không tính booking này</param>
        /// <returns></returns>
        public int RoomCount(RoomClass roomClass, RoomTypex roomType, Cruise cruise, DateTime date, int duration,
                             Booking exception)
        {
            // Điều kiện là cùng một loại kiểu phòng và trên cùng tàu
            ICriterion sametype = Restrictions.And(Restrictions.Eq("RoomClass", roomClass),
                                                 Restrictions.Eq("RoomType", roomType));
            sametype = Restrictions.And(sametype, Restrictions.Eq("Cruise", cruise));
            sametype = Restrictions.And(Restrictions.Eq("Deleted", false), sametype);

            // Tổng số phòng trên tàu
            int all = _commonDao.CountObjectByCriterion(typeof(Room), sametype);

            // Nếu không có phòng nào, trả về -1
            if (all == 0) return -1;

            // Nếu là ngày đã có khóa            
            if (LockedCheckByDate(cruise, date, date.AddDays(duration - 1), exception) != null)
            {
                return 0;
            }

            int book = 0;

            IList booked = _sailsDao.BookingRoomNotAvaiable(cruise, date, duration, exception);

            int onboard = 0;

            #region -- Lấy số phòng tối đa đã có người trong hành trình --

            // Tính số phòng đã có người theo từng ngày một
            for (int ii = 0; ii < duration - 1; ii++)
            {
                DateTime today = date.AddDays(ii);
                foreach (BookingRoom room in booked)
                {
                    if (room.RoomClass.Id == roomClass.Id && room.RoomType.Id == roomType.Id)
                    {
                        // Nếu là ngày đầu tiên, các booking mới trong ngày hoặc từ trước đều tính (Vì đã lọc chỉ lấy các booking vướng theo
                        // đó đã có điều kiện booking chưa kết thúc. Không có check book kết thúc cho ngày đầu tiên
                        if (ii == 0)
                        {
                            if (room.Book.StartDate <= today)
                            {
                                onboard = AddBookCount(roomType, room, onboard, false); // Tăng thêm onboard
                            }
                        }
                        else
                        {
                            if (room.Book.StartDate == today)
                            {
                                onboard = AddBookCount(roomType, room, onboard, false);
                                // Nếu là người lên tàu thì thêm vào
                            }
                            if (room.Book.EndDate == today)
                            {
                                onboard = AddBookCount(roomType, room, onboard, true);
                                // Nếu là người xuống tàu thì trừ đi
                            }
                        }
                    }
                }

                book = Math.Max(book, onboard); // Số phòng vướng sẽ là max của số phòng bị book từng ngày
            }

            #endregion

            // Nếu là phòng ở ghép phải tính theo số chỗ

            if (roomType.IsShared)
            {
                return (all * roomType.Capacity - book);
            }
            return all - book;
        }

        /// <summary>
        /// Lấy số phòng còn trống theo một ngày cho trước, số chỗ trống đối với phòng shared twin, bao gồm cả booking này
        /// </summary>
        /// <param name="roomClass">Đẳng cấp phòng</param>
        /// <param name="roomType">Loại phòng</param>
        /// <param name="cruise"></param>
        /// <param name="date">Ngày đặt phòng</param>
        /// <param name="duration">Số ngày trống liên tiếp</param>
        /// <param name="exception">Bao gồm cả booking này</param>
        /// <returns></returns>
        public int RoomCountInclude(RoomClass roomClass, RoomTypex roomType, Cruise cruise, DateTime date, int duration,
                                    Booking exception)
        {
            if (LockedCheckByDate(cruise, date).Id > 0)
            {
                return 0;
            }
            ICriterion sametype = Restrictions.And(Restrictions.Eq("RoomClass", roomClass),
                                                 Restrictions.Eq("RoomType", roomType));
            sametype = Restrictions.And(Restrictions.Eq("Deleted", false), sametype);
            int all = _commonDao.CountObjectByCriterion(typeof(Room), sametype);
            if (all == 0) return -1;
            int book = 0;
            IList booked = _sailsDao.BookingRoomNotAvaiable(cruise, date, duration, exception);

            foreach (BookingRoom room in booked)
            {
                if (room.RoomClass.Id == roomClass.Id && room.RoomType.Id == roomType.Id)
                {
                    if (roomType.Id != TWIN)
                    {
                        book++;
                    }
                    else
                    {
                        foreach (Customer customer in room.Customers)
                        {
                            if (customer.Type == CustomerType.Adult)
                            {
                                book++;
                            }
                        }
                    }
                }
            }

            if (roomType.Id == TWIN)
            {
                return (all * 2 - book);
            }
            return all - book;
        }

        /// <summary>
        /// Lấy về danh sách các room
        /// </summary>
        /// <param name="exceptDeleted">Có loại trừ deleted không</param>
        /// <returns></returns>
        public IList RoomGetAll(bool exceptDeleted)
        {
            return _sailsDao.RoomGetAll(exceptDeleted);
        }

        /// <summary>
        /// Lấy về danh sách các room
        /// </summary>
        /// <returns></returns>
        public IList RoomGetAll(Cruise cruise)
        {
            ICriterion criterion;
            if (cruise != null)
            {
                criterion = Restrictions.And(Restrictions.Eq("Cruise", cruise), Restrictions.Eq("Deleted", false));
            }
            else
            {
                criterion = Restrictions.Eq("Deleted", false);
            }
            return _commonDao.GetObjectByCriterion(typeof(Room), criterion);
        }
        public IList<Room> RoomGetAll2(Cruise cruise)
        {
            var cri = _commonDao.OpenSession().CreateCriteria(typeof(Room)); ;

            cri.Add(Restrictions.And(Restrictions.Eq("Cruise", cruise), Restrictions.Eq("Deleted", false)));

            return cri.List<Room>();
        }
        /// <summary>
        /// Lấy về danh sach Room theo class và type
        /// </summary>
        /// <param name="cruise"></param>
        /// <param name="roomClass"></param>
        /// <param name="roomType"></param>
        /// <returns></returns>
        public IList RoomGetBy_ClassType(Cruise cruise, RoomClass roomClass, RoomTypex roomType)
        {
            return _sailsDao.RoomGetBy_ClassType(cruise, roomClass, roomType);
        }

        /// <summary>
        /// Kiểm tra xem có tồn tại phòng tương ứng với type và class hay ko?
        /// </summary>
        /// <param name="roomType"></param>
        /// <param name="roomClass"></param>
        /// <returns></returns>
        public bool RoomCheckExist(RoomTypex roomType, RoomClass roomClass)
        {
            return _sailsDao.RoomCheckExist(roomType, roomClass);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách phòng trong đó có trạng thái avaiable
        /// </summary>
        /// <param name="cruise"></param>
        /// <param name="date"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public IList RoomGetAllWithAvaiableStatus(Cruise cruise, DateTime date, int duration)
        {
            IList all = RoomGetAll(true);
            IList locked = _sailsDao.RoomGetNotAvailable(cruise, date, duration);
            //string notAvailable = string.Empty;

            // Trong mọi trường hợp đều là add all vì đây là để chọn prefer
            // Thứ duy nhất khác là có đánh dấu trạng thái avaiable theo các phòng đã khóa
            //foreach (Room room in locked)
            //{
            //    notAvailable += "#" + room.Id;
            //}
            foreach (Room room in all)
            {
                if (locked.Contains(room))
                {
                    room.IsAvailable = false;
                }
            }
            return all;
        }

        #endregion

        #region -- Sails Trip --

        private int _tripMaxId = -1;

        public void Save(SailsTrip trip)
        {
            trip.ModifiedDate = DateTime.Now;
            trip.CreatedDate = DateTime.Now;
            _commonDao.SaveObject(trip);
        }

        public void Update(SailsTrip trip)
        {
            trip.ModifiedDate = DateTime.Now;
            _commonDao.UpdateObject(trip);
        }

        public void Delete(SailsTrip trip)
        {
            trip.Deleted = true;
            trip.ModifiedDate = DateTime.Now;
            _commonDao.UpdateObject(trip);
        }

        public SailsTrip TripGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(SailsTrip), id) as SailsTrip;
        }

        /// <summary>
        /// Lấy về tất cả các SailsTrip
        /// </summary>
        /// <param name="exceptDeleted">Có loại trừ deleted ko?</param>
        /// <returns></returns>
        public IList TripGetAll(bool exceptDeleted)
        {
            return _sailsDao.TripGetAll(exceptDeleted);
        }


        public int TripMaxId()
        {
            if (_tripMaxId < 0)
            {
                IList list = _commonDao.GetObjectByCriterionPaged(typeof(SailsTrip), null, 0, 1, Order.Desc("Id"));
                if (list.Count > 0)
                {
                    _tripMaxId = ((SailsTrip)list[0]).Id;
                }
                else
                {
                    _tripMaxId = 0;
                }
            }
            return _tripMaxId;
        }

        #endregion

        #region -- Booking --

        public void Save(Booking booking, User user)
        {
            booking.CreatedDate = DateTime.Now;
            booking.ModifiedDate = DateTime.Now;

            // Ngày kết thúc bằng ngày bắt đầu + số ngày trừ đi 1 (từ 12h trưa đến 12h trưa hôm sau là 2 ngày)
            booking.EndDate = booking.StartDate.AddDays(booking.Trip.NumberOfDay - 1);
            _commonDao.SaveObject(booking);

            if (user != null)
            {
                var history = new BookingHistory();
                history.Booking = booking;
                history.Date = DateTime.Now;
                history.Agency = booking.Agency;
                history.StartDate = booking.StartDate;
                history.Status = booking.Status;
                history.Trip = booking.Trip;
                history.User = user;
                history.Total = booking.Total;
                history.TotalCurrency = booking.IsTotalUsd ? "USD" : "VND";
                if (booking.BookingRooms != null && booking.BookingRooms.Count > 0) history.CabinNumber = booking.BookingRooms.Count;
                if (booking.BookingRooms != null && booking.BookingRooms.Count > 0) history.CustomerNumber = booking.Pax;

                _commonDao.SaveObject(history);
            }
        }

        public void Update(Booking booking, User user)
        {
            if (user != null)
            {
                booking.ModifiedDate = DateTime.Now;
                booking.ModifiedBy = user;
            }

            if (booking.Trip != null)
            {
                booking.EndDate = booking.StartDate.AddDays(booking.Trip.NumberOfDay - 1);
            }

            if (user != null)
            {
                var history = new BookingHistory();
                history.Booking = booking;
                history.Date = DateTime.Now;
                history.Agency = booking.Agency;
                history.StartDate = booking.StartDate;
                history.Status = booking.Status;
                history.Trip = booking.Trip;
                history.User = user;
                history.Total = booking.Total;
                history.TotalCurrency = booking.IsTotalUsd ? "USD" : "VND";
                if (booking.BookingRooms != null && booking.BookingRooms.Count > 0) history.CabinNumber = booking.BookingRooms.Count;
                if (booking.BookingRooms != null && booking.BookingRooms.Count > 0) history.CustomerNumber = booking.Pax;
                _commonDao.SaveObject(history);
            }

            _commonDao.UpdateObject(booking);
        }

        public void Delete(Booking booking)
        {
            booking.Deleted = true;
            booking.ModifiedDate = DateTime.Now;
            _commonDao.UpdateObject(booking);
        }

        public Booking BookingGetById(int id)
        {
            Booking booking = _commonDao.GetObjectById(typeof(Booking), id) as Booking;
            // Nếu tồn tại booking và trạng thái booking là cancelled
            if (booking != null && booking.Status == StatusType.Cancelled)
            {
                if (booking.IsDirty)
                {
                    SaveOrUpdate(booking);
                }
            }
            return booking;
        }

        public Booking BookingGetByCode(int code)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("CustomBookingId", 0), Restrictions.Eq("Id", code));
            criterion = Restrictions.Or(Restrictions.Eq("CustomBookingId", code), criterion);
            IList list = _commonDao.GetObjectByCriterion(typeof(Booking), criterion);
            if (list.Count > 0)
            {
                if (list.Count > 1)
                {
                    throw new Exception("More than one booking with code " + code);
                }
                return list[0] as Booking;
            }
            return null;
        }

        public bool CheckCustomBookingCode(int code, Booking booking)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("CustomBookingId", code),
                                                  Restrictions.Not(Restrictions.Eq("Id", booking.Id)));
            return _commonDao.CountObjectByCriterion(typeof(Booking), criterion) == 0;
        }

        public IList BookingGetAll(bool exceptDeleted)
        {
            return _sailsDao.BookingGetAll(exceptDeleted);
        }

        /// <summary>
        /// Tìm kiếm Booking
        /// </summary>
        /// <returns></returns>
        public IList BookingSearchFromQueryString(NameValueCollection queryString, bool useCustomBookingId, int pageSize, int pageIndex, out int count)
        {
            ICriterion criterion = Restrictions.Gt("Id", 0);

            #region Deleted

            if (!string.IsNullOrEmpty(queryString["Deleted"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("Deleted", false));
            }

            #endregion

            #region Trip

            int tryParse;
            if (!string.IsNullOrEmpty(queryString["TripId"]) && Int32.TryParse(queryString["TripId"], out tryParse))
            {
                if (tryParse > 0)
                {
                    criterion = Restrictions.And(criterion, Restrictions.Eq("Trip", TripGetById(tryParse)));
                }
            }

            #endregion

            #region Cruise

            if (!string.IsNullOrEmpty(queryString["cruiseid"]))
            {
                criterion = Restrictions.And(criterion,
                                           Restrictions.Eq("Cruise",
                                                         CruiseGetById(Convert.ToInt32(queryString["cruiseid"]))));
            }

            #endregion

            #region Partner

            if (!string.IsNullOrEmpty(queryString["agencyid"]) &&
                Int32.TryParse(queryString["agencyid"], out tryParse))
            {
                if (tryParse > 0)
                {
                    criterion = Restrictions.And(criterion, Restrictions.Eq("Agency.Id", tryParse));
                }
            }

            #endregion

            #region Sale

            if (!string.IsNullOrEmpty(queryString["SaleId"]) && Int32.TryParse(queryString["SaleId"], out tryParse))
            {
                if (tryParse > 0)
                {
                    criterion = Restrictions.And(criterion, Restrictions.Eq("SaleId", UserGetById(tryParse)));
                }
            }

            #endregion

            #region Status

            if (!string.IsNullOrEmpty(queryString["Status"]))
            {
                switch (queryString["Status"])
                {
                    case "0":
                        criterion = Restrictions.And(criterion, Restrictions.Eq("Status", (StatusType)0));
                        break;
                    case "1":
                        criterion = Restrictions.And(criterion, Restrictions.Eq("Status", (StatusType)1));
                        break;
                    case "2":
                        criterion = Restrictions.And(criterion, Restrictions.Eq("Status", (StatusType)2));
                        break;
                    case "3":
                        criterion = Restrictions.And(criterion, Restrictions.Eq("Status", (StatusType)3));
                        break;
                    default:
                        break;
                }
            }

            if (!string.IsNullOrEmpty(queryString["Accounting"]))
            {
                int accounting;
                if (int.TryParse(queryString["Accounting"], out accounting))
                {
                    if (accounting > 0)
                    {
                        criterion = Restrictions.And(criterion,
                                                   Restrictions.Eq("AccountingStatus", (AccountingStatus)accounting));
                    }
                    else
                    {
                        criterion = Restrictions.And(criterion, Restrictions.Or(Restrictions.IsNull("AccountingStatus"),
                                                                            Restrictions.Eq("AccountingStatus",
                                                                                          (AccountingStatus)accounting)));
                    }
                }
            }

            #endregion

            #region StartDate

            if (!string.IsNullOrEmpty(queryString["StartDate"]))
            {
                try
                {
                    CultureInfo cultureInfo = new CultureInfo("vi-VN");

                    DateTime dateTime = DateTime.ParseExact(queryString["StartDate"], "dd/MM/yyyy", cultureInfo.DateTimeFormat);
                    criterion = Restrictions.And(criterion, Restrictions.Eq("StartDate", dateTime));
                }
                catch (Exception)
                {
                    criterion = Restrictions.And(criterion, Restrictions.Eq("StartDate", DateTime.Today));
                }
            }

            #endregion

            #region -- booking id --

            if (queryString["Booking"] != null)
            {
                string bookingid = queryString["Booking"];
                if (!useCustomBookingId)
                {
                    criterion = AddBookingCodeCriterion(criterion, bookingid);
                }
                else
                {
                    criterion = Restrictions.And(criterion,
                                               Restrictions.Like("CustomBookingCode", bookingid, MatchMode.Anywhere));
                }
            }

            #endregion

            #region -- customer or agency name --

            string customer = string.Empty;
            if (queryString["Customer"] != null)
            {
                customer = queryString["Customer"];
            }

            #endregion

            #region -- charter --

            if (queryString["charter"] != null)
            {
                bool charter = queryString["charter"] == "1";
                criterion = Restrictions.And(criterion, Restrictions.Eq("IsCharter", charter));
            }

            #endregion

            #region -- blocked --

            int blocked = 0;
            if (queryString["blocked"] == "1")
            {
                blocked = 1;
            }

            #endregion

            #region -- Is transferred --

            if (queryString["transfer"] != null)
            {
                int transfer = Convert.ToInt32(queryString["transfer"]);
                if (transfer == 0)
                {
                    criterion = Restrictions.And(criterion, Restrictions.Eq("IsTransferred", false));
                }
            }

            #endregion

            #region -- Order --

            Order order = null;
            if (!string.IsNullOrEmpty(queryString["Order"]))
            {
                order = new Order(queryString["Order"].Substring(3),
                                  queryString["Order"].Substring(0, 3).ToLower() == "asc");
            }
            if (order == null)
            {
                order = Order.Desc("StartDate");
            }

            #endregion

            #region -- Voucher batch --
            if (queryString["batchid"] != null)
            {
                var batchid = Convert.ToInt32(queryString["batchid"]);
                criterion = Restrictions.And(criterion, Restrictions.Eq("Batch.Id", batchid));
            }
            #endregion

            count = _sailsDao.BookingCount(criterion, customer, blocked);
            return _sailsDao.BookingSearch(criterion, customer, blocked, pageSize, pageIndex, order);
        }

        /// <summary>
        /// Lấy tất cả booking xuất phát từ ngày xác định
        /// </summary>
        /// <param name="date">Ngày xuất phát</param>
        /// <param name="cruise">Tàu</param>
        /// <param name="penaltyIncluded"></param>
        /// <returns></returns>
        public IList BookingGetByStartDate(DateTime date, Cruise cruise, bool penaltyIncluded)
        {
            ICriterion criterion;
            if (penaltyIncluded)
            {
                criterion = IncomeCriterion();
            }
            else
            {
                // Chưa xóa và đã approved
                criterion = Restrictions.Eq("Deleted", false);
                criterion = Restrictions.And(criterion, Restrictions.Eq("Status", StatusType.Approved));
            }

            criterion = Restrictions.And(criterion, Restrictions.Eq("StartDate", date));
            if (cruise != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("Cruise", cruise));
            }
            return _commonDao.GetObjectByCriterion(typeof(Booking), criterion, Order.Asc("Cruise"));
        }
        /// <summary>
        /// lấy booking theo ngày và tàu
        /// </summary>
        /// <param name="date"></param>
        /// <param name="cruise"></param>
        /// <param name="penaltyIncluded"></param>
        /// <returns></returns>
        public IList<Booking> BookingGetByStartDateAndCruise(DateTime date, Cruise cruise, bool penaltyIncluded)
        {
            var criteria = _commonDao.OpenSession().CreateCriteria(typeof(Booking));
            if (penaltyIncluded)
            {
                criteria.Add(IncomeCriterion());
            }
            else
            {
                // Chưa xóa và đã approved
                criteria.Add(Restrictions.Eq("Deleted", false));
                criteria.Add(Restrictions.Eq("Status", StatusType.Approved));
            }

            criteria.Add(Restrictions.Eq("StartDate", date));
            if (cruise != null)
            {
                criteria.Add(Restrictions.Eq("Cruise.Id", cruise.Id));
            }
            return criteria.List<Booking>();
        }
        /// <summary>
        /// lấy booking theo thời gian và tàu
        /// </summary>
        /// <param name="date"></param>
        /// <param name="cruise"></param>
        /// <returns></returns>
        public IList<Booking> GetBookingByDate(DateTime date, Cruise cruise)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<Booking>();
            //            queryOver.Where(r => (r.EndDate > date.AddDays(1) && r.EndDate < date.AddDays(1) || (r.StartDate > date.AddDays(-1) && r.StartDate < date.AddDays(1))));
            queryOver.Where(r => (r.EndDate > date && r.StartDate > date.AddDays(-1) && r.StartDate < date.AddDays(1)) || (r.StartDate < date && r.EndDate > date));
            queryOver.Where(r => r.Cruise.Id == cruise.Id);
            queryOver.Where(r => r.Status != StatusType.Cancelled);

            return queryOver.List<Booking>().Where(b => b.Status != StatusType.Cancelled).ToList();
        }
        /// <summary>
        /// lấy booking theo thời gian và tàu
        /// </summary>
        /// <param name="date"></param>
        /// <param name="cruiseId"></param>
        /// <returns></returns>
        public IList<Booking> GetBookingByDate(DateTime date, int cruiseId)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<Booking>();
            //            queryOver.Where(r => (r.EndDate > date.AddDays(1) && r.EndDate < date.AddDays(1) || (r.StartDate > date.AddDays(-1) && r.StartDate < date.AddDays(1))));
            queryOver.Where(r => (r.EndDate > date && r.StartDate > date.AddDays(-1) && r.StartDate < date.AddDays(1)) || (r.StartDate < date && r.EndDate > date));
            queryOver.Where(r => r.Cruise.Id == cruiseId);
            queryOver.Where(r => r.Status != StatusType.Cancelled);

            return queryOver.List<Booking>().Where(b => b.Status != StatusType.Cancelled).ToList();
        }
        /// <summary>
        /// check phòng đã được sử dụng hay chưa
        /// </summary>
        /// <param name="rooms">list room</param>
        /// <param name="_cruise">tàu</param>
        /// <param name="startDate">ngày</param>
        /// <param name="isTrip3D">là trip 3 ngày 2 đêm</param>
        /// <returns></returns>
        public bool CheckExistAddRoom(List<Room> rooms, Cruise _cruise, DateTime startDate, bool isTrip3D)
        {
            var allbk = GetBookingByDate(startDate, _cruise);
            var checkAvailable = true;
            foreach (Booking booking in allbk)
            {
                if (isTrip3D)
                {
                    foreach (BookingRoom bookingRoom in booking.BookingRooms)
                    {
                        if (rooms.Contains(bookingRoom.Room))
                        {
                            checkAvailable = false;
                            break;
                        }
                    }
                }
                else
                {
                    if (booking.StartDate.Date == startDate.Date && booking.Status != StatusType.Cancelled)
                    {
                        foreach (BookingRoom bookingRoom in booking.BookingRooms)
                        {
                            if (rooms.Contains(bookingRoom.Room))
                            {
                                checkAvailable = false;
                                break;
                            }
                        }
                    }
                }
            }
            return checkAvailable;
        }
        /// <summary>
        /// lấy booking theo ngày, tàu
        /// </summary>
        /// <param name="date"></param>
        /// <param name="cruise"></param>
        /// <returns></returns>
        public IList<Booking> GetBookingServiceByDate(DateTime date, Cruise cruise)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<Booking>();
            queryOver.Where(r => (r.EndDate > date.AddDays(-1) && r.EndDate <= date.AddDays(1) || (r.StartDate > date.AddDays(-1) && r.StartDate < date.AddDays(1))));
            queryOver.Where(r => r.Cruise.Id == cruise.Id);
            queryOver.Where(r => r.Status == StatusType.Approved);
            return queryOver.List<Booking>();
        }
        /// <summary>
        /// lấy phiếu xuất mới nhất
        /// </summary>
        /// <param name="room">phòng</param>
        /// <param name="date">ngày</param>
        /// <returns></returns>
        public IvExport GetNewestExportService(Room room, DateTime date)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<IvExport>();
            queryOver.Where(r => r.Room.Id == room.Id && r.CreatedDate < date.AddDays(1));
            return queryOver.OrderBy(r => r.CreatedDate).Desc.Take(1).SingleOrDefault();
        }
        /// <summary>
        /// lấy toàn bộ phiếu xuất
        /// </summary>
        /// <param name="room"></param>
        /// <param name="bookingRoom"></param>
        /// <returns></returns>
        public IList<IvExport> GetAllExportService(Room room, BookingRoom bookingRoom)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<IvExport>();
            queryOver.Where(r => r.Room.Id == room.Id && r.BookingRoom.Id == bookingRoom.Id);
            return queryOver.OrderBy(r => r.CreatedDate).Desc.List<IvExport>();
        }
        /// <summary>
        /// lấy toàn bộ phiếu xuất
        /// </summary>
        /// <param name="bookingRoom"></param>
        /// <returns></returns>
        public IList<IvExport> GetAllExportService(BookingRoom bookingRoom)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<IvExport>();
            queryOver.Where(r => r.BookingRoom.Id == bookingRoom.Id);
            return queryOver.OrderBy(r => r.CreatedDate).Desc.List<IvExport>();
        }
        /// <summary>
        /// lấy thông tin phòng đặt theo ngày
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="cruiseId"></param>
        /// <returns></returns>
        public IList<vBookingRoom> GetBookingRoomByStartDate(DateTime startDate, int cruiseId)
        {
            //ICriterion criterion = Restrictions.And(Restrictions.Gt("StartDate", startDate.AddDays(-1)), Restrictions.Lt("StartDate", startDate.AddDays(1)));
            //criterion = Restrictions.Or(criterion,
            //    Restrictions.And(Restrictions.Gt("EndDate", startDate.AddDays(-1)),
            //        Restrictions.Lt("EndDate", startDate.AddDays(1))));
            //criterion = Restrictions.And(criterion, Restrictions.Eq("CruiseId", cruiseId));
            //var criteria = _commonDao.OpenSession().CreateCriteria(typeof(vBookingRoom));

            //criteria.Add(criterion);
            //return criteria.List<vBookingRoom>();
            var queryOver = _commonDao.OpenSession().QueryOver<vBookingRoom>();
            queryOver.Where(r => (r.EndDate > startDate.AddDays(-1) && r.EndDate < startDate.AddDays(1) || (r.StartDate > startDate.AddDays(-1) && r.StartDate < startDate.AddDays(1))));
            queryOver.Where(r => r.CruiseId == cruiseId);
            return queryOver.List<vBookingRoom>();
        }

        public int CountRoomByDateAndCruise(DateTime startDate, int cruiseId)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<vBookingRoom>();
            queryOver.Where(r => (r.EndDate > startDate.AddDays(-1) && r.EndDate < startDate.AddDays(1) || (r.StartDate > startDate.AddDays(-1) && r.StartDate < startDate.AddDays(1))));
            queryOver.Where(r => r.CruiseId == cruiseId);
            queryOver.Select(Projections.CountDistinct("RoomId"));
            return queryOver.RowCount();
        }
        public static ICriterion AddBookingCodeCriterion(ICriterion sourceCri, string bookingid)
        {
            ICriterion criterion = null; // Điều kiện booking code
            if (bookingid != "OS")
            {
                // Có ba khả năng: bắt đầu bằng O hoặc S, bắt đầu bằng số 0 và bắt đầu bằng số thường                
                string str = string.Empty;
                ICriterion exactCrit = null; // Điều kiện bằng chính xác
                bool is0 = true;
                switch (bookingid[0])
                {
                    case 'O':
                        str = bookingid.Substring(2);
                        is0 = false;
                        break;
                    case 'S':
                        str = bookingid.Substring(1);
                        is0 = false;
                        break;
                    case '0':
                        str = bookingid;
                        break;
                    default:
                        exactCrit = Restrictions.Like("BookingId", bookingid, MatchMode.Anywhere);
                        break;
                }

                //Nếu không phải chỉ có điều kiện chính xác
                if (!string.IsNullOrEmpty(str))
                {
                    // Kể cả có 0 ở đầu vẫn search bình thường
                    if (is0)
                    {
                        exactCrit = Restrictions.Like("BookingId", bookingid, MatchMode.Anywhere);
                    }

                    int count = 0;
                    // Dựa trên số lượng số 0, khống chế khoảng xác định
                    while (str.Length > 0 && str[0] == '0')
                    {
                        count++;
                        str = str.Remove(0, 1);
                    }
                    count = 5 - count; // số chữ số tối đa
                    int max = (int)Math.Pow(10, count) - 1;
                    // 10^x là số nhỏ nhất có x + 1 chữ số, - 1 là số lớn nhất có x chữ số

                    const int min = 1;
                    // Không có giới hạn dưới
                    ICriterion rangeCrit = Restrictions.And(Restrictions.Ge("Id", min), Restrictions.Le("Id", max));
                    // Điều kiện theo khoảng

                    if (!string.IsNullOrEmpty(str))
                    {
                        rangeCrit = Restrictions.And(rangeCrit, Restrictions.Like("BookingId", str, MatchMode.Start));
                    }

                    if (exactCrit != null)
                    {
                        criterion = Restrictions.Or(rangeCrit, exactCrit);
                    }
                    else
                    {
                        criterion = rangeCrit;
                    }
                }
                else
                {
                    criterion = exactCrit;
                }
            }

            // Điều kiện về TACode
            ICriterion taCrit = Restrictions.Like("AgencyCode", bookingid, MatchMode.Anywhere);

            ICriterion result;
            if (criterion != null)
            {
                result = Restrictions.Or(criterion, taCrit);
            }
            else
            {
                result = taCrit;
            }

            return Restrictions.And(sourceCri, result);
        }

        public IList BookingGetByCriterion(ICriterion criterion, Order order, out int count, int pageSize, int pageIndex)
        {
            count = _commonDao.CountObjectByCriterion(typeof(Booking), criterion);
            if (pageSize > 0)
            {
                return _sailsDao.BookingSearch(criterion, string.Empty, 0, pageSize, pageIndex, order);
            }
            return _sailsDao.BookingSearch(criterion, string.Empty, 0, order);
        }

        public IList BookingGetByCriterion(ICriterion criterion, Order order, int pageSize, int pageIndex)
        {
            _commonDao.OpenSession();
            if (pageSize > 0)
            {

                return _commonDao.GetObjectByCriterionPaged(typeof(Booking), criterion, pageIndex, pageSize, order);
            }
            return _commonDao.GetObjectByCriterion(typeof(Booking), criterion, order);
        }

        public IList BookingRoomGetByBooking(Booking book)
        {
            if (book != null)
            {
                return _commonDao.GetObjectByProperty(typeof(BookingRoom), "Book", book);
            }
            return new ArrayList();
        }

        public IList GetBookingListInPeriod(bool deleted, StatusType statusType, bool isTransferred, DateTime date)
        {
            var sessionManager = _commonDao;
            var session = sessionManager.OpenSession();
            const string hql = "SELECT b.Id, b.Cruise.Id, br.Id, br.RoomClass, br.RoomType, c.Id, c.Type " +
                         "FROM Booking b JOIN b.BookingRooms br LEFT JOIN br.Customers c " +
                         "WHERE (b.Deleted = :deleted AND b.Status = :statustype) AND b.IsTransferred = :istransferred AND (b.StartDate <= :date AND b.EndDate > :date)";
            var query = session.CreateQuery(hql);
            query.SetParameter("deleted", deleted);
            query.SetParameter("statustype", (int)statusType);
            query.SetParameter("istransferred", isTransferred);
            query.SetParameter("date", date);
            var objectList = query.List();
            var uniqueBookingIdList = new List<int>();
            var uniqueBookingRoomIdList = new List<int>();
            var uniqueCustomerIdList = new List<int>();
            var bookingList = new List<Booking>();
            var bookingRoomList = new List<BookingRoom>();
            var customerList = new List<Customer>();

            for (var i = 0; i < objectList.Count; i++)
            {
                var obj = objectList[i] as Object[];
                if (obj != null)
                {
                    var bookingId = (int)obj[0];
                    var cruiseId = (int)obj[1];
                    var bookingRoomId = (int)obj[2];
                    var roomClass = obj[3] as RoomClass;
                    var roomType = obj[4] as RoomTypex;
                    var customerId = (int)obj[5];
                    var customerType = (CustomerType)obj[6];

                    if (!uniqueBookingIdList.Contains(bookingId))
                    {
                        var cruise = new Cruise { Id = cruiseId };
                        var booking = new Booking { Id = bookingId, Cruise = cruise };
                        uniqueBookingIdList.Add(bookingId);
                        bookingList.Add(booking);
                    }

                    if (!uniqueBookingRoomIdList.Contains(bookingRoomId))
                    {
                        var bookingRoom = new BookingRoom { Id = bookingRoomId, Book = new Booking { Id = bookingId }, RoomClass = roomClass, RoomType = roomType };
                        uniqueBookingRoomIdList.Add(bookingRoomId);
                        bookingRoomList.Add(bookingRoom);
                    }

                    if (!uniqueCustomerIdList.Contains(customerId))
                    {
                        var customer = new Customer { Id = customerId, BookingRoom = new BookingRoom() { Id = bookingRoomId }, Type = customerType };
                        uniqueCustomerIdList.Add(customerId);
                        customerList.Add(customer);
                    }
                }
            }

            var bookingListReturned = new List<Booking>();
            for (var i = 0; i < bookingList.Count; i++)
            {
                var booking = bookingList[i];
                var bookingRoomListInBooking = new List<BookingRoom>();
                for (var j = 0; j < bookingRoomList.Count; j++)
                {
                    var bookingRoom = bookingRoomList[j];
                    if (booking.Id == bookingRoom.Book.Id)
                    {
                        var customerListInBookingRoom = new List<Customer>();
                        for (var k = 0; k < customerList.Count; k++)
                        {
                            var customer = customerList[k];
                            if (customer.BookingRoom.Id == bookingRoom.Id)
                            {
                                customerListInBookingRoom.Add(customer);
                            }
                        }
                        bookingRoom.Customers = customerListInBookingRoom;
                        bookingRoomListInBooking.Add(bookingRoom);
                    }
                }
                booking.BookingRooms = bookingRoomListInBooking;
                bookingListReturned.Add(booking);
            }

            return bookingListReturned;
        }

        public int BookingGenerateCustomId(DateTime startDate)
        {
            // Lay ve booking lon nhat
            int minValue = (startDate.Year - 2000) * 100000 + startDate.Month * 1000 + 1; //10 12 001
            int maxValue = minValue + 998; // 10 12 999
            ICriterion criterion = Restrictions.And(Restrictions.Le("CustomBookingId", maxValue), Restrictions.Ge("CustomBookingId", minValue));
            IList list = _commonDao.GetObjectByCriterionPaged(typeof(Booking), criterion, 0, 1,
                                                              Order.Desc("CustomBookingId")); // Lấy custom bk id lớn nhất nằm trong khoảng quy định
            if (list.Count > 0)
            {
                Booking booking = (Booking)list[0];
                if (booking.Id == maxValue)
                {
                    throw new Exception("Không đủ bộ mã cần thiết (đã tới bk " + booking.Id + ")");
                }
                return booking.Id + 1;
            }
            return minValue;
        }

        /// <summary>
        /// Lấy tất cả các booking đã approved 
        /// </summary>
        /// <param name="cruise"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public IList BookingGetInDate(Cruise cruise, DateTime date)
        {
            // Điều kiện bắt buộc: chưa xóa và có status là Approved
            ICriterion criterion = Restrictions.Eq("Deleted", false);
            criterion = Restrictions.And(criterion, Restrictions.Eq("Status", StatusType.Approved));

            // Không bao gồm booking đã transfer
            criterion = Restrictions.And(criterion, Restrictions.Not(Restrictions.Eq("IsTransferred", true)));

            // Lấy danh sách tàu

            // Điều kiện về tàu
            if (cruise != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("Cruise", cruise));
            }

            criterion = AddDateExpression(criterion, date);
            int count;
            IList list = BookingGetByCriterion(criterion, Order.Asc("Cruise"), out count, 0, 0);
            return list;
        }

        /// <summary>
        /// Lấy về lịch sử booking
        /// </summary>
        /// <param name="booking"></param>
        /// <returns></returns>
        public IList BookingGetHistory(Booking booking)
        {
            return _commonDao.GetObjectByCriterion(typeof(BookingHistory), Restrictions.Eq("Booking", booking), Order.Asc("Date"));
        }

        public IList BookingGetHistoryCabin(Booking booking)
        {
            return _commonDao.GetObjectByCriterion(typeof(BookingHistory), Restrictions.And(Restrictions.Eq("Booking", booking), Restrictions.IsNotNull("CabinNumber")), Order.Asc("Date"));
        }

        #endregion

        #region -- Sail Price Config --

        ///// <summary>
        ///// Lấy về price config theo trip, room type và room class
        ///// </summary>
        ///// <param name="trip"></param>
        ///// <param name="type"></param>
        ///// <param name="roomClass"></param>
        ///// <param name="option"></param>
        ///// <returns></returns>
        //public SailsPriceConfig SailsPriceConfigGetBy_RoomType_RoomClass_Trip(SailsTrip trip, RoomTypex type,
        //                                                                      RoomClass roomClass, TripOption option)
        //{
        //    return _sailsDao.SailsPriceConfigGetBy_RoomType_RoomClass_Trip(trip, type, roomClass, option);
        //}


        public SailsPriceConfig SailsPriceConfigGet(RoomClass roomClass, RoomTypex rtype, SailsTrip trip, Cruise cruise,
                                                    TripOption option, DateTime date, BookingType type, Agency agency)
        {
            SailsPriceTable table = null;
            ICriterion tableCriterion = Restrictions.And(Restrictions.Ge("EndDate", date.Date),
                                                       Restrictions.Le("StartDate", date.Date));
            tableCriterion = Restrictions.And(tableCriterion, Restrictions.IsNull("Cruise"));

            // Bảng giá chỉ quy định thời hạn và role

            //tableCriterion =
            //    Restrictions.And(Restrictions.And(Restrictions.Eq(SailsPriceTable.TRIP, trip),
            //                                  Restrictions.Eq(SailsPriceTable.OPTION, option)), tableCriterion);
            //if (cruise != null)
            //{
            //    tableCriterion = Restrictions.And(tableCriterion, Restrictions.Or(Restrictions.Eq("Cruise", cruise), Restrictions.IsNull("Cruise")));
            //}

            //ICriterion agencyCriterion = Restrictions.Or(Restrictions.Eq("Agency", agency), Restrictions.IsNull("Agency")); // Lấy về bảng giá của Đl hoặc bảng giá chung
            //tableCriterion = Restrictions.And(tableCriterion, agencyCriterion);

            IList tables = _commonDao.GetObjectByCriterion(typeof(SailsPriceTable), tableCriterion, Order.Desc("Agency"), Order.Desc("Cruise"));
            if (tables.Count > 0)
            {
                table = tables[0] as SailsPriceTable;
            }
            if (table == null)
            {
                return null;
            }

            ICriterion criterion = Restrictions.And(Restrictions.Eq("RoomClass", roomClass),
                                                  Restrictions.Eq("RoomType", rtype));
            criterion = Restrictions.And(criterion, Restrictions.Eq("Table", table));
            criterion = Restrictions.And(criterion, Restrictions.Eq("Cruise", cruise));
            criterion = Restrictions.And(criterion, Restrictions.Eq("Trip", trip));
            IList configs = _commonDao.GetObjectByCriterion(typeof(SailsPriceConfig), criterion);
            if (configs.Count > 0)
            {
                return configs[0] as SailsPriceConfig;
            }
            return null;
        }

        public SailsPriceConfig SailsPriceConfigGet2(RoomClass roomClass, RoomTypex rtype, SailsTrip trip, Cruise cruise,
                                                    TripOption option, DateTime date, BookingType type, Agency agency)
        {
            SailsPriceTable table = null;
            ICriterion tableCriterion = Restrictions.And(Restrictions.Ge("EndDate", date.Date),
                                                       Restrictions.Le("StartDate", date.Date));
            // Bảng giá chỉ quy định thời hạn và role


            tableCriterion =
                Restrictions.And(Restrictions.And(Restrictions.Eq("Trip", trip),
                                              Restrictions.Eq("Option", option)), tableCriterion);
            if (cruise != null)
            {
                tableCriterion = Restrictions.And(tableCriterion, Restrictions.Or(Restrictions.Eq("Cruise", cruise), Restrictions.IsNull("Cruise")));
            }

            //ICriterion agencyCriterion = Restrictions.Or(Restrictions.Eq("Agency", agency), Restrictions.IsNull("Agency")); // Lấy về bảng giá của Đl hoặc bảng giá chung
            //tableCriterion = Restrictions.And(tableCriterion, agencyCriterion);

            IList tables = _commonDao.GetObjectByCriterion(typeof(SailsPriceTable), tableCriterion, Order.Desc("Agency"), Order.Desc("Cruise"));
            if (tables.Count > 0)
            {
                table = tables[0] as SailsPriceTable;
            }
            if (table == null)
            {
                return null;
            }

            return SailsPriceConfigGet(table, rtype, roomClass);
            //ICriterion criterion = Restrictions.And(Restrictions.Eq(SailsPriceConfig.ROOMCLASS, roomClass),
            //                                      Restrictions.Eq(SailsPriceConfig.ROOMTYPE, rtype));
            //criterion = Restrictions.And(criterion, Restrictions.Eq(SailsPriceConfig.TABLE, table));
            //criterion = Restrictions.And(criterion, Restrictions.Eq("Cruise", cruise));
            //criterion = Restrictions.And(criterion, Restrictions.Eq("Trip", trip));
            //IList configs = _commonDao.GetObjectByCriterion(typeof(SailsPriceConfig), criterion);
            //if (configs.Count > 0)
            //{
            //    return configs[0] as SailsPriceConfig;
            //}
            //return null;
        }

        /// <summary>
        /// Lấy về giá theo kiểu bảng truyền thống
        /// </summary>
        /// <param name="table"></param>
        /// <param name="rtype"></param>
        /// <param name="rclass"></param>
        /// <returns></returns>
        public SailsPriceConfig SailsPriceConfigGet(SailsPriceTable table, RoomTypex rtype, RoomClass rclass)
        {
            if (table != null && table.Id > 0)
            {
                ICriterion criterion = Restrictions.And(Restrictions.Eq("RoomClass", rclass),
                                                      Restrictions.Eq("RoomType", rtype));
                criterion = Restrictions.And(criterion, Restrictions.Eq("Table", table));
                IList list = _commonDao.GetObjectByCriterion(typeof(SailsPriceConfig), criterion);
                if (list.Count > 0)
                {
                    return list[0] as SailsPriceConfig;
                }
                return new SailsPriceConfig();
            }
            return new SailsPriceConfig();
        }

        public SailsPriceConfig SailsPriceConfigGet(SailsPriceTable table, RoomTypex rtype, RoomClass rclass, SailsTrip trip, Cruise cruise, TripOption option)
        {
            if (table != null && table.Id > 0)
            {
                ICriterion criterion = Restrictions.And(Restrictions.Eq("RoomClass", rclass),
                                                      Restrictions.Eq("RoomType", rtype));
                criterion = Restrictions.And(criterion, Restrictions.Eq("Table", table));

                criterion = Restrictions.And(criterion, Restrictions.Eq("Cruise", cruise));
                criterion = Restrictions.And(criterion, Restrictions.Eq("Trip", trip));
                criterion = Restrictions.And(criterion, Restrictions.Eq("TripOption", option));

                IList list = _commonDao.GetObjectByCriterion(typeof(SailsPriceConfig), criterion);
                if (list.Count > 0)
                {
                    return list[0] as SailsPriceConfig;
                }
                return new SailsPriceConfig();
            }
            return new SailsPriceConfig();
        }

        public void SaveOrUpdate(SailsPriceConfig priceConfig)
        {
            if (priceConfig.Id > 0)
            {
                _commonDao.UpdateObject(priceConfig);
            }
            else
            {
                _commonDao.SaveObject(priceConfig);
            }
        }

        public SailsPriceConfig SailsPriceConfigGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(SailsPriceConfig), id) as SailsPriceConfig;
        }

        #endregion

        #region -- Agency --

        /// <summary>
        /// Lưu thông tin loại đại lý
        /// </summary>
        /// <param name="agency">loại đại lý cần lưu</param>
        public void Save(AgencyPolicy agency)
        {
            _commonDao.SaveObject(agency);
        }

        /// <summary>
        /// Cập nhật thông tin loại đại lý
        /// </summary>
        /// <param name="agency">loại đại lý cần lưu</param>
        public void Update(AgencyPolicy agency)
        {
            _commonDao.UpdateObject(agency);
        }

        /// <summary>
        /// Xóa thông tin loại đại lý
        /// </summary>
        /// <param name="agency">loại đại lý cần xóa</param>
        public void Delete(AgencyPolicy agency)
        {
            _commonDao.DeleteObject(agency);
        }

        public Role RoleGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(Role), id) as Role;
        }

        /// <summary>
        /// Lấy về đại lý theo role cho trước
        /// </summary>
        /// <param name="role">Role đại lý lấy về</param>
        /// <returns>AgencyPolicy</returns>
        public IList AgencyPolicyGetByRole(Role role)
        {
            if (role != null)
            {
                return _sailsDao.AgencyPolicyGetByRole(role);
            }
            IList list = new ArrayList();
            AgencyPolicy policy = new AgencyPolicy();
            policy.CostFrom = 0;
            policy.CostTo = null;
            policy.Percentage = 100;
            policy.IsPercentage = true;
            list.Add(policy);
            return list;
        }

        public AgencyPolicy AgencyPolicyGetById(int id)
        {
            return (AgencyPolicy)_commonDao.GetObjectById(typeof(AgencyPolicy), id);
        }

        public IList RoleGetAll()
        {
            return _commonDao.GetAll(typeof(Role));
        }

        public int UserCount()
        {
            return _sailsDao.UserCount();
        }

        public IList AgencyGetWithLastBooking(ICriterion criterion, params Order[] orders)
        {
            return _sailsDao.AgencyGetWithLastBooking(criterion, orders);
        }

        public IList AgencyGetReceivable(params Order[] orders)
        {
            ICriterion criterion = Restrictions.Gt("Total", (double)0);
            return _sailsDao.AgencyGetWithLastBooking(criterion, orders);
        }

        public IList AgencyGetPayable(params Order[] orders)
        {
            ICriterion criterion = Restrictions.Gt("Payable", (double)0);
            return _sailsDao.AgencyGetWithLastBooking(criterion, orders);
        }

        /// <summary>
        /// Hàm thực hiện đầy đủ các chính sách sau:
        /// 1. Các đối tác Never gửi booking kể từ ngày tạo sau 4 tháng sẽ được chuyển cho Unbound Sales
        /// 2. Các đối tác không gửi booking kể từ ngày chuyển/tạo booking cuối sẽ được chuyển cho Unbound Sales
        /// </summary>
        /// <returns></returns>
        public IList<vAgency> AgencyResetSale(bool reset = true)
        {
            var result = new List<vAgency>();

            // Lấy về các đối tác never mà lại có nhân viên sale, và ngày chuyển lớn hơn 4 tháng, và 
            ICriterion criterion = Restrictions.IsNull("LastBooking");
            criterion = Restrictions.And(criterion, Restrictions.IsNotNull("Sale"));
            criterion = Restrictions.And(criterion, Restrictions.Lt("SaleStart", DateTime.Now.AddMonths(-4))); // giao cho sale trước 4 tháng
            criterion = Restrictions.And(criterion, Restrictions.Lt("CreatedDate", DateTime.Now.AddMonths(-4))); //create trước 4 tháng
            var list = _sailsDao.AgencyGetWithLastBooking(criterion); // never
            foreach (vAgency agency in list)
            {
                if (reset)
                {
                    agency.Agency.Sale = null;
                    _commonDao.UpdateObject(agency.Agency);
                }
                result.Add(agency);
            }

            // Lấy về các đối tác booking cuối lớn hơn 6 tháng và ngày chuyển cuối lớn hơn 6 tháng
            criterion = Restrictions.And(criterion, Restrictions.IsNotNull("Sale")); // hiện có sale in charge
            criterion = Restrictions.Lt("LastBooking", DateTime.Now.AddMonths(-6)); // còn trước cả 6 tháng trước
            criterion = Restrictions.And(criterion, Restrictions.Lt("SaleStart", DateTime.Now.AddMonths(-6)));

            list = _sailsDao.AgencyGetWithLastBooking(criterion); // never
            foreach (vAgency agency in list)
            {
                if (reset)
                {
                    agency.Agency.Sale = null;
                    _commonDao.UpdateObject(agency.Agency);
                }
                result.Add(agency);
            }

            return result;
        }

        public IList<Agency> AgencyRecheck()
        {
            var result = new List<Agency>();

            // Lấy về tất cả các agency unbound để check lại
            ICriterion criterion = Restrictions.IsNull("Sale");
            criterion = Restrictions.And(criterion, Restrictions.Ge("SaleStart", DateTime.Now.AddMonths(-5)));

            var list = _commonDao.GetObjectByCriterion(typeof(vAgency), criterion);
            foreach (vAgency agency in list)
            {
                // nếu agency được tạo sau 4 tháng hoặc chuyển trước 4 tháng kể từ thời điểm hiện tại
                if (agency.SaleStart > DateTime.Now.AddMonths(-4) || agency.CreatedDate > DateTime.Now.AddMonths(-4))
                {
                    // phục hồi lại sale này theo sale mới nhất
                    AgencyHistory value = null;
                    foreach (AgencyHistory history in agency.Agency.History)
                    {
                        if (value == null)
                        {
                            value = history;
                        }
                        else if (history.ApplyFrom > value.ApplyFrom)
                        {
                            value = history;
                        }
                    }
                    if (value != null)
                    {
                        agency.Agency.Sale = value.Sale;
                        _commonDao.UpdateObject(agency.Agency);
                        result.Add(agency.Agency);
                    }
                }
            }

            return result;
        }

        #endregion

        #region -- Extra Option --

        public void Save(BookingExtra customerExtraOption)
        {
            _commonDao.SaveObject(customerExtraOption);
        }

        public void Save(ExtraOption extraOption)
        {
            _commonDao.SaveObject(extraOption);
        }

        public void Update(ExtraOption extraOption)
        {
            _commonDao.UpdateObject(extraOption);
        }

        public void Delete(ExtraOption extraOption)
        {
            _commonDao.DeleteObject(extraOption);
        }

        public ExtraOption ExtraOptionGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(ExtraOption), id) as ExtraOption;
        }

        public IList ExtraOptionGetBooking()
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("Deleted", false),
                                                  Restrictions.Eq("Target", ServiceTarget.Booking));
            return _commonDao.GetObjectByCriterion(typeof(ExtraOption), criterion);
        }

        public IList ExtraOptionGetCustomer()
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("Deleted", false),
                                                  Restrictions.Eq("Target", ServiceTarget.Customer));
            return _commonDao.GetObjectByCriterion(typeof(ExtraOption), criterion);
        }

        public IList ExtraOptionGetAll()
        {
            ICriterion criterion = Restrictions.Eq("Deleted", false);
            return _commonDao.GetObjectByCriterion(typeof(ExtraOption), criterion);
        }

        public BookingExtra BookingExtraGet(Booking booking, ExtraOption option)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("Booking", booking),
                                                  Restrictions.Eq("ExtraOption", option));
            IList list = _commonDao.GetObjectByCriterion(typeof(BookingExtra), criterion);
            if (list.Count > 0)
            {
                return list[0] as BookingExtra;
            }
            return new BookingExtra(booking, option);
        }

        #endregion

        #region -- Customer --

        public Customer CustomerGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(Customer), id) as Customer;
        }

        public void Save(Customer customer)
        {
            customer.Birthday = DateTime.Today;
            _commonDao.SaveObject(customer);
        }

        public void Update(Customer customer)
        {
            if (customer.Id > 0 && string.IsNullOrEmpty(customer.Code))
            {
                customer.Code = customer.Id.ToString();
            }
            _commonDao.UpdateObject(customer);
        }

        public void SaveOrUpdate(Customer customer)
        {
            if (customer.Id > 0 && string.IsNullOrEmpty(customer.Code))
            {
                customer.Code = customer.Id.ToString();
            }
            _commonDao.SaveOrUpdateObject(customer);
        }

        public void Delete(Customer customer)
        {
            _commonDao.DeleteObject(customer);
        }

        /// <summary>
        /// Lấy về các Customer của 1 booking
        /// </summary>
        /// <param name="booking"></param>
        /// <returns></returns>
        public IList<Customer> CustomerGetByBooking(Booking booking)
        {
            return _sailsDao.CustomerGetByBooking(booking);
        }

        public IList CustomerGetAllDistinct()
        {
            return _sailsDao.CustomerGetAllDistinct();
            //return _commonDao.GetAll(typeof (Customer));
        }

        public IList CustomerGetDistinctByQueryString(NameValueCollection queryString, int pageSize, int pageIndex,
                                                      out int count)
        {
            ICriterion criterion = Restrictions.Not(Restrictions.Eq("Code", ""));

            if (queryString["name"] != null)
            {
                criterion = Restrictions.And(criterion,
                                           Restrictions.Like("Fullname", queryString["name"], MatchMode.Anywhere));
            }

            if (queryString["birth"] != null)
            {
                DateTime date = DateTime.FromOADate(Convert.ToDouble(queryString["birth"]));
                criterion = Restrictions.And(criterion, Restrictions.Eq("Birthday", date));
            }

            if (queryString["passport"] != null)
            {
                criterion = Restrictions.And(criterion,
                                           Restrictions.Like("Passport", queryString["passport"], MatchMode.Anywhere));
            }

            if (queryString["gender"] != null)
            {
                int gender = Convert.ToInt32(queryString["gender"]);
                if (gender == 1)
                {
                    criterion = Restrictions.And(criterion, Restrictions.Eq("IsMale", true));
                }
                else
                {
                    criterion = Restrictions.And(criterion, Restrictions.Eq("IsMale", false));
                }
            }

            if (queryString["nationality"] != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Country", queryString["nationality"]));
            }

            if (queryString["bookingid"] != null)
            {
                Booking bk = BookingGetById(Convert.ToInt32(queryString["bookingid"]));
                if (bk != null)
                {
                    criterion = Restrictions.And(criterion, Restrictions.Eq("Booking", bk));
                }
            }

            if (queryString["bookingcode"] != null)
            {
                Booking bk = BookingGetByCode(Convert.ToInt32(queryString["bookingcode"]));
                if (bk != null)
                {
                    criterion = Restrictions.And(criterion, Restrictions.Eq("Booking", bk));
                }
            }

            return _sailsDao.CustomerGetByCriterionPaged(criterion, pageSize, pageIndex, out count);
        }

        #endregion

        #region -- Booking Room --

        public void Save(BookingRoom bookingRoom)
        {
            _commonDao.SaveObject(bookingRoom);
        }

        public void Update(BookingRoom bookingRoom)
        {
            _commonDao.UpdateObject(bookingRoom);
        }

        /// <summary>
        /// Xóa bỏ một phòng đã đặt
        /// </summary>
        /// <param name="bookingRoom">Phòng xóa</param>
        /// <param name="user">Người thực hiện thao tác xóa</param>
        public void Delete(BookingRoom bookingRoom, User user)
        {
            BookingTrack track = new BookingTrack();
            track.Booking = bookingRoom.Book;
            track.ModifiedDate = DateTime.Now;
            track.User = user;
            _commonDao.SaveObject(track);

            BookingChanged changed = new BookingChanged();
            changed.Track = track;
            changed.Action = BookingAction.RemoveRoom;
            changed.Parameter = string.Format("{0}-{1}", bookingRoom.RoomType.Name, bookingRoom.RoomClass.Name);
            _commonDao.DeleteObject(bookingRoom);
        }

        public BookingRoom BookingRoomGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(BookingRoom), id) as BookingRoom;
        }

        #endregion

        #region -- Agency Policy --

        public double ApplyPriceFor(double originPrice, IList agencyList)
        {
            foreach (AgencyPolicy policy in agencyList)
            {
                if (originPrice >= policy.CostFrom && (policy.CostTo == null || originPrice <= policy.CostTo))
                {
                    if (policy.IsPercentage)
                    {
                        return Math.Round(originPrice * policy.Percentage / 100, 0);
                    }
                    //if (target.Id != 1)
                    //{
                    //    return Math.Round(originPrice + policy.Percentage * target.Rate);
                    //}
                    return Math.Round(originPrice + policy.Percentage);
                }
            }
            return originPrice;
        }

        public CruiseRole GetCruiseRole(Cruise cruise, Agency agency)
        {
            ICriterion crit = Restrictions.And(Restrictions.Eq("Cruise", cruise), Restrictions.Eq("Agency", agency));
            IList list = _commonDao.GetObjectByCriterion(typeof(CruiseRole), crit);
            if (list.Count > 0)
            {
                return list[0] as CruiseRole;
            }
            return new CruiseRole();
        }

        #endregion

        #region -- User --

        public User UserGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(User), id) as User;
        }

        public IList UserGetAll()
        {
            return _commonDao.GetAll(typeof(User));
        }

        public IList GetUsersActive()
        {
            ICriterion criterion = Restrictions.Eq("IsActive", true);
            return _commonDao.GetObjectByCriterion(typeof(User), criterion);
        }

        public IList GetUsersInActive()
        {
            ICriterion criterion = Restrictions.Eq("IsActive", false);
            return _commonDao.GetObjectByCriterion(typeof(User), criterion);
        }
        #endregion

        #region -- Price Table --

        public IList PriceTableGetAll(SailsTrip trip, TripOption option, Cruise cruise, Agency agency)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("Trip", trip),
                                                  Restrictions.Eq("Option", option));
            if (agency != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("Agency", agency));
            }
            else
            {
                criterion = Restrictions.And(criterion, Restrictions.IsNull("Agency"));
            }
            if (cruise != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("Cruise", cruise));
            }
            return _commonDao.GetObjectByCriterion(typeof(SailsPriceTable), criterion);
        }

        public SailsPriceTable PriceTableGetById(int id)
        {
            if (id > 0)
            {
                return _commonDao.GetObjectById(typeof(SailsPriceTable), id) as SailsPriceTable;
            }
            return new SailsPriceTable();
        }

        public void SaveOrUpdate(SailsPriceTable table)
        {
            _commonDao.SaveOrUpdateObject(table);
        }

        public IList GetPriceTable(Role role)
        {
            ICriterion criterion = Restrictions.Eq("Role", role);
            return _commonDao.GetObjectByCriterion(typeof(SailsPriceTable), criterion);
        }

        #endregion

        #region -- Expense --

        public IList ExpenseGetByDate(Cruise cruise, DateTime from, DateTime to)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Ge("Date", from),
                                                  Restrictions.Le("Date", to));
            if (cruise != null)
            {
                criterion = Restrictions.And(Restrictions.Eq("Cruise", cruise), criterion);
            }
            else
            {
                criterion = Restrictions.And(Restrictions.IsNull("Cruise"), criterion);
            }
            return _commonDao.GetObjectByCriterion(typeof(Expense), criterion, Order.Asc("Date"));
        }

        /// <summary>
        /// Luôn trả về expense, tạo mới nếu chưa có
        /// </summary>
        /// <param name="cruise"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public Expense ExpenseGetByDate(Cruise cruise, DateTime date)
        {
            ICriterion criterion = Restrictions.Eq("Date", date);
            if (cruise != null)
            {
                criterion = Restrictions.And(Restrictions.Eq("Cruise", cruise), criterion);
            }
            else
            {
                criterion = Restrictions.And(Restrictions.IsNull("Cruise"), criterion);
            }

            IList list = _commonDao.GetObjectByCriterion(typeof(Expense), criterion);
            if (list != null && list.Count > 0)
            {
                return list[0] as Expense;
            }
            Expense expense = new Expense();
            expense.Date = date;
            expense.Cruise = cruise;
            return expense;
        }

        public void SaveOrUpdate(object obj)
        {
            _commonDao.SaveOrUpdateObject(obj);
        }
        public void Update(object obj)
        {
            _commonDao.UpdateObject(obj);
        }
        public void SaveOrUpdate(object obj, User user)
        {
            PropertyInfo identity = obj.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            int id = (int)identity.GetValue(obj, null);
            PropertyInfo by;
            PropertyInfo date;
            if (id > 0)
            {
                date = obj.GetType().GetProperty("ModifiedDate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                by = obj.GetType().GetProperty("ModifiedBy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            }
            else
            {
                date = obj.GetType().GetProperty("CreatedDate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                by = obj.GetType().GetProperty("CreatedBy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            }

            if (date != null)
            {
                date.SetValue(obj, DateTime.Now, null);
            }

            if (by != null)
            {
                by.SetValue(obj, user, null);
            }

            _commonDao.SaveOrUpdateObject(obj);
        }

        public SailExpensePayment PaymentGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(SailExpensePayment), id) as SailExpensePayment;
        }

        public void IncomeSum(DateTime from, DateTime to, out double income, out double receivable)
        {
            _sailsDao.IncomeSum(from, to, out income, out receivable);
        }

        public void OutcomeSum(DateTime from, DateTime to, out double outcome, out double payable)
        {
            _sailsDao.OutcomeSum(from, to, out outcome, out payable);
        }

        #endregion

        #region -- Sale --

        public Sale SaleGetByUser(User user)
        {
            IList list = _commonDao.GetObjectByCriterion(typeof(Sale), Restrictions.Eq("User", user));
            if (list.Count > 0)
            {
                return list[0] as Sale;
            }
            return null;
        }

        public Sale SaleGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(Sale), id) as Sale;
        }

        public IList SalesGetAll()
        {
            return _commonDao.GetAll(typeof(Sale));
        }

        #endregion

        #region -- Settings --

        public void SaveModuleSetting(string key, string value)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq(SystemSetting.MODULETYPE, Section.ModuleType),
                                                  Restrictions.Eq(SystemSetting.KEY, key));
            IList list = _commonDao.GetObjectByCriterion(typeof(SystemSetting), criterion);
            if (list.Count > 0)
            {
                SystemSetting setting = (SystemSetting)list[0];
                setting.Value = value;
                _commonDao.SaveOrUpdateObject(setting);
            }
            else
            {
                SystemSetting setting = new SystemSetting();
                setting.ModuleType = Section.ModuleType;
                setting.Key = key;
                setting.Value = value;
                _commonDao.SaveOrUpdateObject(setting);
            }
        }

        public object ModuleSettings(string key)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq(SystemSetting.MODULETYPE, Section.ModuleType),
                                                  Restrictions.Eq(SystemSetting.KEY, key));
            IList list = _commonDao.GetObjectByCriterion(typeof(SystemSetting), criterion);
            if (list.Count > 0)
            {
                SystemSetting setting = (SystemSetting)list[0];
                return setting.Value;
            }
            return null;
        }

        #endregion

        #region -- Criterion support --

        private IList _trips;

        public IList AllTrip
        {
            get
            {
                if (_trips == null)
                {
                    _trips = TripGetAll(true);
                }
                return _trips;
            }
        }

        /// <summary>
        /// Thêm điều kiện về ngày
        /// </summary>
        /// <param name="criterion">Điều kiện gốc</param>
        /// <param name="date">Ngày</param>
        /// <returns></returns>
        public ICriterion AddDateExpression(ICriterion criterion, DateTime date)
        {
            ICriterion dateCrit = null;
            dateCrit = Restrictions.And(Restrictions.Le("StartDate", date), Restrictions.Gt("EndDate", date));
            //foreach (SailsTrip trip in AllTrip)
            //{
            //    //TODO: Đổi lại thành ngày kết thúc
            //    DateTime up;
            //    if (trip.NumberOfDay > 1)
            //    {
            //        up = date.AddDays(-trip.NumberOfDay + 2);
            //    }
            //    else
            //    {
            //        up = date;
            //    }

            //    ICriterion crit = Restrictions.And(Restrictions.Eq(Booking.TRIP, trip),
            //                                     Restrictions.And(
            //                                         Restrictions.Ge(Booking.STARTDATE, up),
            //                                         Restrictions.Le(Booking.STARTDATE, date)));

            //    if (dateCrit != null)
            //    {
            //        dateCrit = Restrictions.Or(dateCrit, crit);
            //    }
            //    else
            //    {
            //        dateCrit = crit;
            //    }
            //}
            if (dateCrit != null)
            {
                return Restrictions.And(criterion, dateCrit);
            }
            return criterion;
        }

        #endregion

        #region -- Agency --

        public IList AgencyGetByName(string name)
        {
            return _commonDao.GetObjectByCriterion(typeof(Agency), Restrictions.Like("Name", name, MatchMode.Anywhere),
                                                   Order.Asc("Name"));
        }

        public Agency AgencyGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(Agency), id) as Agency;
        }

        public IList AgencyGetAll()
        {
            return _commonDao.GetAll(typeof(Agency), "Name");
        }

        public IList SupplierGetAll()
        {
            if (ModuleSettings("Suppliers") != null)
            {
                Role role = RoleGetById(Convert.ToInt32(ModuleSettings("Suppliers")));
                ICriterion criterion = Restrictions.Eq("Role", role);
                return _commonDao.GetObjectByCriterion(typeof(Agency), criterion);
            }
            return _commonDao.GetAll(typeof(Agency));
        }

        public IList GuidesGetAll()
        {
            if (ModuleSettings("Guides") != null)
            {
                Role role = RoleGetById(Convert.ToInt32(ModuleSettings("Guides")));
                ICriterion criterion = Restrictions.And(Restrictions.Eq("Role", role), Restrictions.Eq("Deleted", false));
                return _commonDao.GetObjectByCriterion(typeof(Agency), criterion);
            }
            return _commonDao.GetAll(typeof(Agency));
        }

        public IList AgencyGetByQueryString(NameValueCollection query, int pageSize, int pageIndex, out int count)
        {
            ICriterion criterion = Restrictions.Or(Restrictions.Eq("Deleted", false), Restrictions.IsNull("Deleted"));
            if (query["Name"] != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Name", query["Name"], MatchMode.Anywhere));
            }

            if (query["RoleId"] != null)
            {
                if (criterion != null)
                {
                    criterion = Restrictions.And(criterion,
                                               Restrictions.Eq("Role", RoleGetById(Convert.ToInt32(query["RoleId"]))));
                }
                else
                {
                    criterion = Restrictions.Eq("Role", RoleGetById(Convert.ToInt32(query["RoleId"])));
                }
            }

            if (query["saleid"] != null)
            {
                int saleid = Convert.ToInt32(query["saleid"]);
                if (saleid > 0)
                {
                    User sale = UserGetById(saleid);
                    if (criterion != null)
                    {
                        criterion = Restrictions.And(criterion, Restrictions.Eq("Sale", sale));
                    }
                    else
                    {
                        criterion = Restrictions.Eq("Sale", sale);
                    }

                }
                else
                {
                    if (criterion != null)
                    {
                        criterion = Restrictions.And(criterion, Restrictions.IsNull("Sale"));
                    }
                    else
                    {
                        criterion = Restrictions.IsNull("Sale");
                    }
                }
            }
            count = _commonDao.CountObjectByCriterion(typeof(Agency), criterion);
            return _commonDao.GetObjectByCriterionPaged(typeof(Agency), criterion, pageIndex, pageSize, Order.Asc("Name"));
        }

        public IList vAgencyGetByQueryString(NameValueCollection query, int pageSize, int pageIndex, out int count, User user = null)
        {
            ICriterion criterion = null;
            if (query["Name"] != null)
            {
                criterion = Restrictions.Like("Name", query["Name"], MatchMode.Anywhere);
            }

            if (query["contract"] != null)
            {
                if (criterion != null)
                {
                    criterion = Restrictions.And(criterion,
                                               Restrictions.Eq("ContractStatus", Convert.ToInt32(query["contract"])));
                }
                else
                {
                    criterion = Restrictions.Eq("ContractStatus", Convert.ToInt32(query["contract"]));
                }
            }

            if (query["RoleId"] != null)
            {
                if (criterion != null)
                {
                    criterion = Restrictions.And(criterion,
                                               Restrictions.Eq("Role", RoleGetById(Convert.ToInt32(query["RoleId"]))));
                }
                else
                {
                    criterion = Restrictions.Eq("Role", RoleGetById(Convert.ToInt32(query["RoleId"])));
                }
            }

            if (query["locationid"] != null)
            {
                if (criterion != null)
                {
                    criterion = Restrictions.And(criterion,
                                               Restrictions.Eq("Location", GetObject<AgencyLocation>(Convert.ToInt32(query["locationid"]))));
                }
                else
                {
                    criterion = Restrictions.Eq("Location", GetObject<AgencyLocation>(Convert.ToInt32(query["locationid"])));
                }
            }

            if (query["saleid"] != null)
            {
                int saleid = Convert.ToInt32(query["saleid"]);
                if (saleid > 0)
                {
                    User sale = UserGetById(saleid);
                    if (criterion != null)
                    {
                        criterion = Restrictions.And(criterion, Restrictions.Eq("Sale", sale));
                    }
                    else
                    {
                        criterion = Restrictions.Eq("Sale", sale);
                    }

                }
                else
                {
                    if (criterion != null)
                    {
                        criterion = Restrictions.And(criterion, Restrictions.IsNull("Sale"));
                    }
                    else
                    {
                        criterion = Restrictions.IsNull("Sale");
                    }
                }

                //User sale = UserGetById(saleid);
                //if (sale != null)
                //{
                //    var agencyIdList = new List<int>();
                //    var vAgencys = _commonDao.GetAll(typeof(vAgency));
                //    foreach (vAgency vAgency in vAgencys)
                //    {
                //        var _current = new DateTime();
                //        AgencyHistory currentSalesInCharge = null;
                //        foreach (AgencyHistory history in vAgency.Agency.History)
                //        {
                //            if (history.ApplyFrom > _current && history.ApplyFrom < DateTime.Today)
                //            {
                //                _current = history.ApplyFrom;
                //                currentSalesInCharge = history;
                //                if (currentSalesInCharge.Sale == sale)
                //                {
                //                    agencyIdList.Add(currentSalesInCharge.Agency.Id);
                //                }
                //            }
                //        }
                //    }
                //    criterion = Restrictions.In("Id", agencyIdList);
                //}

            }

            if (criterion == null)
            {
                if (!PermissionCheck(Permission.VIEW_ALL_AGENCY, user))
                    criterion = Restrictions.Eq("Sale", user);
            }

            count = _commonDao.CountObjectByCriterion(typeof(vAgency), criterion);
            return _commonDao.GetObjectByCriterionPaged(typeof(vAgency), criterion, pageIndex, pageSize, Order.Asc("Name"));
        }

        public void Delete(object obj)
        {
            _commonDao.DeleteObject(obj);
        }

        public AgencyUser AgencyUserGet(Agency agency, User user)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("Agency", agency), Restrictions.Eq("User", user));
            IList list = _commonDao.GetObjectByCriterion(typeof(AgencyUser), criterion);
            if (list.Count > 0)
            {
                return list[0] as AgencyUser;
            }
            AgencyUser data = new AgencyUser();
            data.User = user;
            data.Agency = agency;
            _commonDao.SaveObject(data);
            return data;
        }

        #endregion

        #region -- Locked --

        public Locked LockedCheckByDate(Cruise cruise, DateTime date)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Le("Start", date), Restrictions.Ge("End", date));
            criterion = Restrictions.And(criterion, Restrictions.Eq("Cruise", cruise));
            IList list = _commonDao.GetObjectByCriterion(typeof(Locked), criterion);
            if (list.Count > 0)
            {
                return list[0] as Locked;
            }
            Locked locked = new Locked();
            locked.Cruise = cruise;
            locked.Start = date;
            locked.End = date;
            locked.Description = string.Empty;
            return locked;
        }

        /// <summary>
        /// Kiểm tra trong khoảng thời gian (diễn ra hành trình) có bất kỳ lock nào hay không
        /// </summary>
        /// <param name="cruise">Tàu kiểm tra</param>
        /// <param name="from">Thời gian bắt đầu</param>
        /// <param name="to">Thời gian kết thúc</param>
        /// <returns></returns>
        public Locked LockedCheckByDate(Cruise cruise, DateTime from, DateTime to)
        {
            ICriterion criterionFrom = Restrictions.And(Restrictions.Le("Start", from), Restrictions.Ge("End", from));
            ICriterion criterionTo = Restrictions.And(Restrictions.Lt("Start", to), Restrictions.Gt("End", to));

            ICriterion criterion = Restrictions.Or(criterionFrom, criterionTo);
            criterion = Restrictions.And(criterion, Restrictions.Eq("Cruise", cruise));

            //IList list = _commonDao.GetObjectByCriterion(typeof(Locked), criterion);

            var crit = _commonDao.OpenSession().CreateCriteria(typeof(Locked));
            crit.CreateAlias("Charter", "booking");
            crit.Add(Restrictions.Not(Restrictions.Eq("booking.Status", StatusType.Cancelled)));
            crit.Add(criterion);
            IList list = crit.List();
            if (list.Count > 0)
            {
                return list[0] as Locked;
            }
            return null;
        }

        public Locked LockedCheckByDate(Cruise cruise, DateTime from, DateTime to, Booking exception)
        {
            ICriterion criterionFrom = Restrictions.And(Restrictions.Le("Start", from), Restrictions.Ge("End", from));
            ICriterion criterionTo = Restrictions.And(Restrictions.Lt("Start", to), Restrictions.Gt("End", to));

            ICriterion criterion = Restrictions.Or(criterionFrom, criterionTo);
            criterion = Restrictions.And(criterion, Restrictions.Eq("Cruise", cruise));
            //IList list = _commonDao.GetObjectByCriterion(typeof(Locked), criterion);
            var crit = _commonDao.OpenSession().CreateCriteria(typeof(Locked));
            crit.CreateAlias("Charter", "booking");
            crit.Add(Restrictions.Not(Restrictions.Eq("booking.Status", StatusType.Cancelled)));
            crit.Add(criterion);
            IList list = crit.List();

            ICriterion bookingCriterionFrom = Restrictions.And(Restrictions.Le("StartDate", from), Restrictions.Ge("EndDate", from));
            ICriterion bookingCriterionTo = Restrictions.And(Restrictions.Lt("StartDate", to), Restrictions.Gt("EndDate", to));

            ICriterion bookingCriterion = Restrictions.Or(bookingCriterionFrom, bookingCriterionTo);
            bookingCriterion = Restrictions.And(bookingCriterion, Restrictions.Eq("Cruise", cruise));
            bookingCriterion = Restrictions.And(bookingCriterion, Restrictions.Not(Restrictions.Eq("Status", StatusType.Cancelled)));

            IList bookingList = _commonDao.GetObjectByCriterion(typeof(Booking), bookingCriterion);

            if (bookingList.Count > 0)
            {
                if (((Booking)bookingList[0]).Id == exception.Id)
                {
                    return null;
                }
            }

            if (list.Count > 0)
            {
                return list[0] as Locked;
            }
            return null;
        }

        public bool LockedCheckCharter(Locked locked)
        {
            ICriterion criterion = Restrictions.Eq("Charter", locked);
            int count = _commonDao.CountObjectByCriterion(typeof(Booking), criterion);
            return count > 0;
        }

        #endregion

        #region -- Cruise table --

        public IList CruiseTableGetAll()
        {
            return _commonDao.GetAll(typeof(CruiseExpenseTable));
        }

        /// <summary>
        /// Lấy về bảng giá thuê tàu theo thời hạn xác định
        /// </summary>
        /// <param name="date"></param>
        /// <param name="cruise"></param>
        /// <returns></returns>
        public CruiseExpenseTable CruiseTableGetValid(DateTime date, Cruise cruise)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Ge("ValidTo", date), Restrictions.Le("ValidFrom", date));
            if (cruise != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("Cruise", cruise));
            }
            IList list = _commonDao.GetObjectByCriterion(typeof(CruiseExpenseTable), criterion, Order.Asc("ValidFrom"));
            if (list.Count > 0)
            {
                return list[0] as CruiseExpenseTable;
            }
            return null;
        }

        public CruiseExpenseTable CruiseTableGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(CruiseExpenseTable), id) as CruiseExpenseTable;
        }

        //public SupplierService SupplierServiceGetById(int id)
        //{
        //    return _commonDao.GetObjectById(typeof (SupplierService), id) as SupplierService;
        //}

        public ExpenseService ExpenseServiceGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(ExpenseService), id) as ExpenseService;
        }

        public USDRate ExchangedGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(USDRate), id) as USDRate;
        }

        public IList ExchangedGetAll()
        {
            return _commonDao.GetAll(typeof(USDRate));
        }

        public USDRate ExchangeGetByDate(DateTime date)
        {
            ICriterion criterion = Restrictions.Le("ValidFrom", date);
            IList list = _commonDao.GetObjectByCriterion(typeof(USDRate), criterion, Order.Desc("ValidFrom"));
            if (list.Count > 0)
            {
                return list[0] as USDRate;
            }
            return null;
        }

        #endregion

        #region -- Costing --

        public CostingTable CostingTableGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(CostingTable), id) as CostingTable;
        }

        public Costing CostingGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(Costing), id) as Costing;
        }

        public IList CostingTableGetAll()
        {
            return _commonDao.GetAll(typeof(CostingTable));
        }

        public DailyCostTable DailyCostTableGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(DailyCostTable), id) as DailyCostTable;
        }

        public DailyCost DailyCostGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(DailyCost), id) as DailyCost;
        }

        public IList DailyCostTableGetAll()
        {
            return _commonDao.GetAll(typeof(DailyCostTable));
        }

        /// <summary>
        /// Lấy về bảng giá xác định tại thời điểm cho trước, đồng thời tính luôn ngày hết hạn
        /// </summary>
        /// <param name="date">Ngày cần xác định bảng giá</param>
        /// <returns></returns>
        public DailyCostTable DailyCostTableGetValid(DateTime date)
        {
            ICriterion criterion = Restrictions.Le("ValidFrom", date);
            // Ngày hợp lệ thấp hơn ngày hiện tại và là cao nhất (gần đây nhất)
            IList list = _commonDao.GetObjectByCriterionPaged(typeof(DailyCostTable), criterion, 0, 1,
                                                              Order.Desc("ValidFrom"));
            if (list.Count > 0)
            {
                DailyCostTable result = (DailyCostTable)list[0];
                // Ngày hết hạn là ngày bắt đầu tính theo giá mới tiếp theo
                ICriterion validToCrit = Restrictions.Gt("ValidFrom", date);
                list = _commonDao.GetObjectByCriterionPaged(typeof(DailyCostTable), validToCrit, 0, 1,
                                                            Order.Asc("ValidFrom"));
                if (list.Count > 0)
                {
                    result.ValidTo = ((DailyCostTable)list[0]).ValidFrom.AddDays(-1);
                    // Hết hạn trước thời hạn mới 1 ngày
                }
                return result;
            }
            return null;
        }

        public IList CostingTableGetValid(DateTime date)
        {
            ICriterion criterion = Restrictions.Le("ValidFrom", date);
            return _commonDao.GetObjectByCriterion(typeof(CostingTable), criterion, Order.Desc("ValidFrom"));
        }

        public CostingTable CostingTableGetValid(DateTime date, SailsTrip trip, TripOption option)
        {
            ICriterion criterion = Restrictions.Le("ValidFrom", date);
            ICriterion tripCriterion;
            if (trip.NumberOfOptions > 1)
            {
                tripCriterion = Restrictions.And(Restrictions.Eq("Trip", trip), Restrictions.Eq("Option", option));
            }
            else
            {
                tripCriterion = Restrictions.Eq("Trip", trip);
            }
            criterion = Restrictions.And(criterion, tripCriterion);

            // Ngày hợp lệ thấp hơn ngày hiện tại và là cao nhất (gần đây nhất)
            IList list = _commonDao.GetObjectByCriterionPaged(typeof(CostingTable), criterion, 0, 1,
                                                              Order.Desc("ValidFrom"));
            if (list.Count > 0)
            {
                CostingTable result = (CostingTable)list[0];
                // Ngày hết hạn là ngày bắt đầu tính theo giá mới tiếp theo
                ICriterion validToCrit = Restrictions.Gt("ValidFrom", date);
                list = _commonDao.GetObjectByCriterionPaged(typeof(CostingTable), validToCrit, 0, 1,
                                                            Order.Asc("ValidFrom"));
                if (list.Count > 0)
                {
                    result.ValidTo = ((CostingTable)list[0]).ValidFrom.AddDays(-1);
                    // Hết hạn trước thời hạn mới 1 ngày
                }
                return result;
            }
            return null;
        }

        #endregion

        #region -- Cost --

        public CostType CostTypeGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(CostType), id) as CostType;
        }

        public IList CostTypeGetAll()
        {
            return _commonDao.GetAll(typeof(CostType));
        }

        /// <summary>
        /// Lấy các chi phí không phải chi phí theo tháng hay năm
        /// </summary>
        /// <returns></returns>
        public IList CostTypeGetDailyCost()
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("IsMonthly", false), Restrictions.Eq("IsYearly", false));
            return _commonDao.GetObjectByCriterion(typeof(CostType), criterion, Order.Desc("IsSupplier"));
        }

        public IList CostTypeGetNotInput()
        {
            ICriterion criterion = Restrictions.Eq("IsDailyInput", false);
            criterion = Restrictions.And(criterion,
                                       Restrictions.And(Restrictions.Eq("IsMonthly", false),
                                                      Restrictions.Eq("IsYearly", false)));
            return _commonDao.GetObjectByCriterion(typeof(CostType), criterion, Order.Desc("IsSupplier"));
        }

        /// <summary>
        /// Lấy về danh sách giá tính trên đầu khách
        /// </summary>
        /// <returns></returns>
        public IList CostTypeGetCustomerBase()
        {
            ICriterion criterion = Restrictions.Eq("IsDailyInput", false); // Không phải nhập thủ công
            criterion = Restrictions.And(criterion,
                                       Restrictions.And(Restrictions.Eq("IsMonthly", false),
                                                      Restrictions.Eq("IsYearly", false))); // Không tính theo tháng/ năm
            criterion = Restrictions.And(criterion, Restrictions.Eq("IsDaily", false)); // Đồng thời ko theo chuyến
            return _commonDao.GetObjectByCriterion(typeof(CostType), criterion, Order.Desc("IsSupplier"));
        }

        /// <summary>
        /// Lấy về danh sách giá tính theo chuyến (chi phí tự động)
        /// </summary>
        /// <returns></returns>
        public IList CostTypeGetAutoDailyBase()
        {
            ICriterion criterion = Restrictions.Eq("IsDailyInput", false);
            criterion = Restrictions.And(criterion,
                                       Restrictions.And(Restrictions.Eq("IsMonthly", false),
                                                      Restrictions.Eq("IsYearly", false)));
            criterion = Restrictions.And(criterion, Restrictions.Eq("IsDaily", true));
            return _commonDao.GetObjectByCriterion(typeof(CostType), criterion, Order.Desc("IsSupplier"));
        }

        private bool? _hasRunningCost;

        public bool HasRunningCost
        {
            get
            {
                if (!_hasRunningCost.HasValue)
                {
                    IList list = CostTypeGetAutoDailyBase();
                    _hasRunningCost = list.Count > 0;
                }
                return _hasRunningCost.Value;
            }
        }

        /// <summary>
        /// Lấy về danh sách các chi phí phải nhập thủ công theo ngày
        /// </summary>
        /// <returns></returns>
        public IList CostTypeGetDailyInput()
        {
            ICriterion criterion = Restrictions.Eq("IsDailyInput", true);
            criterion = Restrictions.And(criterion,
                                       Restrictions.And(Restrictions.Eq("IsMonthly", false),
                                                      Restrictions.Eq("IsYearly", false)));
            return _commonDao.GetObjectByCriterion(typeof(CostType), criterion, Order.Desc("IsSupplier"));
        }

        /// <summary>
        /// Lấy về chi phí phải nhập thủ công theo tháng
        /// </summary>
        /// <returns></returns>
        public IList CostTypeGetMonthly()
        {
            ICriterion criterion = Restrictions.Eq("IsDailyInput", true);
            criterion = Restrictions.And(criterion,
                                       Restrictions.And(Restrictions.Eq("IsMonthly", true),
                                                      Restrictions.Eq("IsYearly", false)));
            return _commonDao.GetObjectByCriterion(typeof(CostType), criterion, Order.Asc("GroupName"));
        }

        /// <summary>
        /// Lấy về chi phí phải nhập thủ công theo tháng
        /// </summary>
        /// <returns></returns>
        public IList CostTypeGetYearly()
        {
            ICriterion criterion = Restrictions.Eq("IsDailyInput", true);
            criterion = Restrictions.And(criterion,
                                       Restrictions.And(Restrictions.Eq("IsMonthly", false),
                                                      Restrictions.Eq("IsYearly", true)));
            return _commonDao.GetObjectByCriterion(typeof(CostType), criterion, Order.Desc("IsSupplier"));
        }

        /// <summary>
        /// Lấy toàn bộ chi phí trong khoảng từ ngày đến ngày theo một loại chi phí nhất định
        /// </summary>
        /// <param name="cruise"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="agency"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public IList ExpenseServiceGet(Cruise cruise, DateTime? from, DateTime? to, Agency agency, CostType type)
        {
            return _sailsDao.ExpenseServiceGet(cruise, from, to, type, agency);
        }

        public ExpenseService ExpenseServiceGetLastest(DateTime date, CostType type)
        {
            ICriterion criterion = Restrictions.Eq("Type", type);
            IList list = _commonDao.GetObjectByCriterionPaged(typeof(ExpenseService), criterion, 0, 1, Order.Desc("Id"));
            if (list.Count > 0)
            {
                return list[0] as ExpenseService;
            }
            return null;
        }

        /// <summary>
        /// Tạo một sao chép chi phí tháng cho ngày đích
        /// </summary>
        /// <param name="expense"></param>
        /// <returns></returns>
        public double CopyMonthlyCost(Expense expense)
        {
            Dictionary<CostType, ExpenseService> serviceMap = new Dictionary<CostType, ExpenseService>();
            IList costs = CostTypeGetMonthly();
            IList expenses = expense.Services;
            double total = 0;

            // Kiểm tra xem đã có giá các dịch vụ nào
            foreach (ExpenseService service in expenses)
            {
                if (!serviceMap.ContainsKey(service.Type) && costs.Contains(service.Type))
                {
                    serviceMap.Add(service.Type, service);
                    total += service.Cost;
                }
            }

            foreach (CostType type in costs)
            {
                if (serviceMap.ContainsKey(type))
                {
                    continue;
                }
                ExpenseService lastService = ExpenseServiceGetLastest(expense.Date, type);
                ExpenseService newService = new ExpenseService();
                newService.Expense = expense;
                if (lastService != null)
                {
                    newService.Cost = lastService.Cost;
                }
                else
                {
                    newService.Cost = 0;
                }
                newService.Name = string.Format("{0} - {1}/{2}", type.Name, expense.Date.Month, expense.Date.Year);
                newService.Supplier = type.DefaultAgency;
                newService.Type = type;
                SaveOrUpdate(newService);
                total += newService.Cost;
            }
            return total;
        }

        /// <summary>
        /// Tạo một sao chép chi phí tháng cho ngày đích
        /// </summary>
        /// <param name="expense"></param>
        /// <returns></returns>
        public double CopyYearlyCost(Expense expense)
        {
            Dictionary<CostType, ExpenseService> serviceMap = new Dictionary<CostType, ExpenseService>();
            IList costs = CostTypeGetYearly();
            IList expenses = expense.Services;
            double total = 0;

            // Kiểm tra xem đã có giá các dịch vụ nào
            foreach (ExpenseService service in expenses)
            {
                if (!serviceMap.ContainsKey(service.Type) && costs.Contains(service.Type))
                {
                    serviceMap.Add(service.Type, service);
                    total += service.Cost;
                }
            }

            foreach (CostType type in costs)
            {
                if (serviceMap.ContainsKey(type))
                {
                    continue;
                }
                ExpenseService lastService = ExpenseServiceGetLastest(expense.Date, type);
                ExpenseService newService = new ExpenseService();
                newService.Expense = expense;
                newService.Cost = lastService.Cost;
                newService.Name = string.Format("{0} - {1}/{2}", type.Name, expense.Date.Month, expense.Date.Year);
                newService.Supplier = type.DefaultAgency;
                newService.Type = type;
                SaveOrUpdate(newService);
                total += newService.Cost;
            }
            return total;
        }

        public int RunningDayCount(Cruise cruise, int year, int month)
        {
            return _sailsDao.RunningDayCount(cruise, year, month);
        }

        #endregion

        #region -- Contact --

        public AgencyContact ContactGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(AgencyContact), id) as AgencyContact;
        }

        public IList ContactGetByAgency(Agency agency, bool booker = false)
        {
            ICriterion criterion = Restrictions.Eq("Agency", agency);

            if (booker)
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("IsBooker", true));
            }

            return _commonDao.GetObjectByCriterion(typeof(AgencyContact), criterion, Order.Desc("Enabled"));
        }

        public IList ContactGetAllEnabled()
        {
            ICriterion criterion = Restrictions.Eq("Enabled", true);
            criterion = Restrictions.And(criterion, Restrictions.Eq("IsBooker", true));
            return _commonDao.GetObjectByCriterion(typeof(AgencyContact), criterion, Order.Asc("Name"));
        }

        #endregion

        #region -- Day Note --

        public DayNote DayNoteGetByDay(Cruise cruise, DateTime date)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("Date", date.Date), Restrictions.Eq("Cruise", cruise));
            IList list = _commonDao.GetObjectByCriterion(typeof(DayNote), criterion);
            if (list.Count > 0)
            {
                return list[0] as DayNote;
            }
            return new DayNote(date);
        }

        public IList DayNoteGetByDay(Cruise cruise, DateTime from, DateTime to)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Ge("Date", from.Date), Restrictions.Le("Date", to.Date));
            if (cruise != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("Cruise", cruise));
            }
            return _commonDao.GetObjectByCriterion(typeof(DayNote), criterion);
        }

        #endregion

        #region -- Tracking --

        public IList TrackingGetByDateRange(DateTime from, DateTime to)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Gt("ModifiedDate", from), Restrictions.Lt("ModifiedDate", to));
            return _commonDao.GetObjectByCriterion(typeof(BookingTrack), criterion, Order.Asc("ModifiedDate"));
        }

        public IList TrackingGetByBooking(Booking booking)
        {
            ICriterion criterion = Restrictions.Eq("Booking", booking);
            return _commonDao.GetObjectByCriterion(typeof(BookingTrack), criterion, Order.Asc("ModifiedDate"));
        }

        public int TrackingCountByBooking(Booking booking)
        {
            ICriterion criterion = Restrictions.Eq("Booking", booking);
            return _commonDao.CountObjectByCriterion(typeof(BookingTrack), criterion);
        }

        public string GetChangeContent(BookingChanged change)
        {
            string[] parameters = change.Parameter.Split('|');
            switch (change.Action)
            {
                case BookingAction.AddCustomer:
                    return string.Format("Thêm vào {0} người lớn, {1} trẻ em, {2} trẻ sơ sinh", parameters);
                case BookingAction.AddRoom:
                    return string.Format("Thêm vào 1 phòng {0}", parameters);
                case BookingAction.Approved:
                    return string.Format("Kích hoạt lại booking hủy");
                case BookingAction.Cancelled:
                    return string.Format("Hủy booking");
                case BookingAction.ChangeDate:
                    return string.Format("Thay đổi ngày xuất phát từ {0:dd/MM/yyyy} sang {1:dd/MM/yyyy}",
                                         DateTime.FromOADate(Convert.ToDouble(parameters[0])),
                                         DateTime.FromOADate(Convert.ToDouble(parameters[1])));
                case BookingAction.ChangeRoomNumber:
                    return string.Format("Chuyển từ phòng {0} sang phòng {1}", parameters);
                case BookingAction.ChangeRoomType:
                    return string.Format("Chuyển từ phòng {0} sang phòng {1}", parameters);
                case BookingAction.ChangeTotal:
                    return string.Format("Thay đổi tổng giá booking");
                case BookingAction.ChangeTrip:
                    return string.Format("Thay đổi hành trình từ {0} thành {1}", parameters);
                case BookingAction.Created:
                    return string.Format("Tạo booking");
                case BookingAction.RemoveCustomer:
                    return string.Format("Bớt đi {0} người lớn, {1} trẻ em, {2} trẻ sơ sinh", parameters);
                case BookingAction.RemoveRoom:
                    return string.Format("Bớt đi 1 phòng {0}", parameters);
                case BookingAction.Transfer:
                    return string.Format("Chuyển sang tàu khác", parameters);
                case BookingAction.Untransfer:
                    return string.Format("Nhận lại từ tàu khác", parameters);
                case BookingAction.ChangeTransfer:
                    return string.Format("Thay đổi giá trị chuyển", parameters);
            }
            return string.Empty;
        }

        #endregion

        #region -- Customer Service --

        public CustomerService CustomerServiceGetByCustomerAndService(Customer customer, ExtraOption service)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("Customer", customer), Restrictions.Eq("Service", service));
            IList list = _commonDao.GetObjectByCriterion(typeof(CustomerService), criterion);
            if (list.Count > 0)
            {
                return list[0] as CustomerService;
            }
            return null;
        }

        public int CustomerServiceCountByBooking(ExtraOption service, Booking booking)
        {
            return _sailsDao.CustomerServiceCountByBooking(service, booking);
        }

        public CustomerService CustomerServiceGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(CustomerService), id) as CustomerService;
        }

        #endregion

        #region -- Workaround --

        private IList _services;

        public IList CustomerServices
        {
            get
            {
                if (_services == null)
                {
                    _services = ExtraOptionGetCustomer();
                }
                return _services;
            }
        }

        public BarRevenue BarRevenueGetByDate(Cruise cruise, DateTime date)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("Date", date), Restrictions.Eq("Cruise", cruise));
            IList list = _commonDao.GetObjectByCriterion(typeof(BarRevenue), criterion);
            if (list.Count > 0)
            {
                return list[0] as BarRevenue;
            }
            BarRevenue revenue = new BarRevenue();
            revenue.Date = date;
            revenue.Cruise = cruise;
            return revenue;
        }

        public double SumBarByDate(DateTime date)
        {
            return _sailsDao.SumBarByDate(date);
        }

        public IList ContractGetByAgency(Agency agency)
        {
            return _commonDao.GetObjectByCriterion(typeof(AgencyContract), Restrictions.Eq("Agency", agency));
        }

        public AgencyContract ContractGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(AgencyContract), id) as AgencyContract;
        }

        #endregion

        #region -- Đếm booking --

        /// <summary>
        /// Đếm booking theo trạng thái
        /// </summary>
        /// <param name="type"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public int CountBookingByStatus(StatusType type, DateTime? date)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("Status", type), Restrictions.Eq("Deleted", false));
            if (date.HasValue)
            {
                criterion = Restrictions.And(criterion, Restrictions.Gt("StartDate", date.Value));
            }
            return _commonDao.CountObjectByCriterion(typeof(Booking), criterion);
        }

        public int CountBookingByCriterion(ICriterion criterion)
        {
            return _sailsDao.BookingCount(criterion);
        }

        /// <summary>
        /// Đếm số booking theo trạng thái charter hay không
        /// </summary>
        /// <param name="isCharter"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public int CountBookingByCharter(bool isCharter, DateTime? date)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("IsCharter", isCharter), Restrictions.Eq("Deleted", false));
            if (date.HasValue)
            {
                criterion = Restrictions.And(criterion, Restrictions.Gt("StartDate", date.Value));
            }
            return _commonDao.CountObjectByCriterion(typeof(Booking), criterion);
        }

        public int CountBookingOnCharter(bool needTransfer, DateTime? date)
        {
            ICriterion criterion = Restrictions.Eq("Deleted", false);
            if (needTransfer)
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("IsTransferred", false));
            }
            if (date.HasValue)
            {
                criterion = Restrictions.And(criterion, Restrictions.Gt("StartDate", date.Value));
            }
            return _sailsDao.BookingCount(criterion, string.Empty, 1);
        }

        public int CountBookingByAccounting(AccountingStatus status, DateTime? date)
        {
            ICriterion criterion = Restrictions.Eq("Deleted", false);
            criterion = Restrictions.And(criterion, Restrictions.Eq("AccountingStatus", status));

            if (date.HasValue)
            {
                criterion = Restrictions.And(criterion, Restrictions.Gt("StartDate", date.Value));
            }
            return _commonDao.CountObjectByCriterion(typeof(Booking), criterion);
        }

        #endregion

        #region -- Services price --

        public IList ServicePriceGetByBooking(Booking booking)
        {
            ICriterion criterion = Restrictions.Eq("Booking", booking);
            return _commonDao.GetObjectByCriterion(typeof(BookingServicePrice), criterion);
        }

        public BookingServicePrice ServicePriceGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(BookingServicePrice), id) as BookingServicePrice;
        }

        #endregion

        #region -- Cruises --

        public Cruise CruiseGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(Cruise), id) as Cruise;
        }

        private IList _allCruises;
        public IList CruiseGetAll()
        {
            _allCruises = _commonDao.GetObjectByCriterion(typeof(Cruise), Restrictions.Eq("Deleted", false));
            return _allCruises;
        }
        /// <summary>
        /// lấy các cruise không bị khóa
        /// </summary>
        /// <returns></returns>
        public IList<Cruise> CruiseGetAllNotLock(DateTime date)
        {
            var query = _commonDao.OpenSession().QueryOver<Cruise>();
            query = query.Where(x => x.Deleted == false);
            query = query.Where(x => x.IsLock == false || (x.IsLock && x.LockType == "From" && x.LockFromDate > date)
                             || (x.IsLock && x.LockType == "FromTo" &&
                                 (x.LockFromDate > date || x.LockToDate < date)));
            return query.List<Cruise>();
        }

        public IList<Cruise> CruiseGetAllNotLock(DateTime date, User user)
        {
            var query = _commonDao.OpenSession().QueryOver<Cruise>();
            query = query.Where(x => x.Deleted == false);
            query = query.Where(x => x.IsLock == false || (x.IsLock && x.LockType == "From" && x.LockFromDate > date)
                             || (x.IsLock && x.LockType == "FromTo" &&
                                 (x.LockFromDate > date || x.LockToDate < date)));
            IvRoleCruise roleCruise = null;
            query = query.JoinAlias(x => x.ListRoleCruises, () => roleCruise);
            query = query.Where(x => roleCruise.User == user || roleCruise.Role.IsIn(user.Roles));
            return query.TransformUsing(Transformers.DistinctRootEntity).List();
        }

        public IList<Cruise> CruiseGetAll2()
        {
            var crit = _commonDao.OpenSession().CreateCriteria(typeof(Cruise));
            crit.Add(Restrictions.Eq("Deleted", false));
            return crit.List<Cruise>();
        }
        public IList<Cruise> CruiseGetByUser(User user)
        {
            if (user.HasPermission(AccessLevel.Administrator))
                return CruiseGetAll2();
            else
            {
                var query = _commonDao.OpenSession().QueryOver<Cruise>();
                query = query.Where(x => x.Deleted == false);
                IvRoleCruise roleCruise = null;
                query = query.JoinAlias(x => x.ListRoleCruises, () => roleCruise);
                query = query.Where(x => roleCruise.User == user || roleCruise.Role.IsIn(user.Roles));
                return query.TransformUsing(Transformers.DistinctRootEntity).List();
            }
        }
        public IList<Cruise> CruiseGetByUserNotLock(User user, DateTime date)
        {
            if (user.HasPermission(AccessLevel.Administrator))
                return CruiseGetAllNotLock(date);
            else
            {
                var query = _commonDao.OpenSession().QueryOver<Cruise>();
                query = query.Where(x => x.Deleted == false);
                IvRoleCruise roleCruise = null;
                query = query.JoinAlias(x => x.ListRoleCruises, () => roleCruise);
                query = query.Where(x => roleCruise.User == user || roleCruise.Role.IsIn(user.Roles));
                return query.TransformUsing(Transformers.DistinctRootEntity).List();
            }
        }
        public void SaveOrUpdate(Cruise cruise, User user)
        {
            if (cruise.Id <= 0)
            {
                cruise.CreatedBy = user;
                cruise.CreatedDate = DateTime.Now;
            }
            cruise.ModifiedBy = user;
            cruise.ModifiedDate = DateTime.Now;
            _commonDao.SaveOrUpdateObject(cruise);
        }

        public CruiseTrip CruiseTripGet(Cruise cruise, SailsTrip trip)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Eq("Cruise", cruise), Restrictions.Eq("Trip", trip));
            IList list = _commonDao.GetObjectByCriterion(typeof(CruiseTrip), criterion);
            if (list.Count > 0)
            {
                return list[0] as CruiseTrip;
            }
            CruiseTrip cr = new CruiseTrip();
            cr.Cruise = cruise;
            cr.Trip = trip;
            return cr;
        }

        #endregion

        #region -- Nationalities and Purposes --
        public Nationality NationalityGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(Nationality), id) as Nationality;
        }

        private IList _nationalities;
        public IList NationalityGetAll()
        {
            if (_nationalities == null)
            {
                _nationalities = _commonDao.GetObjectByCriterion(typeof(Nationality), Restrictions.Eq("Deleted", false),
                                                       Order.Asc("Name"));
            }
            return _nationalities;
        }

        public Nationality NationalityGetByCode(string code)
        {
            IList list = _commonDao.GetObjectByCriterion(typeof(Nationality), Restrictions.Eq("Code", code));
            if (list.Count > 0)
            {
                return list[0] as Nationality;
            }
            return null;
        }

        public Purpose PurposeGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(Purpose), id) as Purpose;
        }

        public Purpose purposeGetByCode(string code)
        {
            IList list = _commonDao.GetObjectByCriterion(typeof(Purpose), Restrictions.Eq("Code", code));
            if (list.Count > 0)
            {
                return list[0] as Purpose;
            }
            return null;
        }

        private IList _purposes;
        public IList PurposeGetAll()
        {
            if (_purposes == null)
            {
                _purposes = _commonDao.GetObjectByCriterion(typeof(Purpose), Restrictions.Eq("Deleted", false),
                                                       Order.Asc("Code"));
            }
            return _purposes;
        }
        #endregion

        #region -- Permission --
        public IList PermissionsGetByRole(Role role)
        {
            return _commonDao.PermissionsGetByRole(Section.ModuleType, role);
        }
        public IList<IvRoleCruise> CruisePermissionsGetByRole(Role role)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<IvRoleCruise>();
            queryOver.Where(r => r.Role == role);
            return queryOver.List<IvRoleCruise>();
        }
        public SpecialPermission PermissionGetByRole(string name, Role role)
        {
            return _commonDao.PermissionGetByRole(name, role);
        }

        public SpecialPermission PermissionGetByUser(string name, User user)
        {
            return _commonDao.PermissionGetByUser(name, user);
        }

        public IList PermissionsGetByUser(User user)
        {
            return _commonDao.PermissionsGetByUser(Section.ModuleType, user);
        }
        public IList<IvRoleCruise> CruisePermissionsGetByUser(User user)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<IvRoleCruise>();
            queryOver.Where(r => r.User == user);
            return queryOver.List<IvRoleCruise>();
        }
        public IList PermissionsGetByUserRole(User user)
        {
            return _commonDao.PermissionsGetByUserRole(Section.ModuleType, user);
        }
        public IList<IvRoleCruise> CruisePermissionsGetByUserRole(User user)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<IvRoleCruise>();
            queryOver.Where(r => r.Role.IsIn(user.Roles));
            return queryOver.List<IvRoleCruise>();
        }

        public bool PermissionCheck(string name, User user)
        {
            if (user.HasPermission(AccessLevel.Administrator))
            {
                return true;// Admin luôn có mọi quyền
            }
            return _commonDao.PermissionCheck(name, user);
        }
        #endregion

        #region -- Booking BLL --
        /// <summary>
        /// Điều kiện xác định Booking có sinh doanh thu
        /// </summary>
        /// <returns></returns>
        public static ICriterion IncomeCriterion()
        {
            ICriterion criterion = Restrictions.Eq("Deleted", false);
            ICriterion approved = Restrictions.Eq("Status", StatusType.Approved);
            ICriterion cancelled = Restrictions.And(Restrictions.Eq("Status", StatusType.Cancelled), Restrictions.Gt("CancelPay", (double)0));

            // Doanh thu bao gồm các BK được approved, hoặc được cancelled nhưng có cancelpay VÀ chưa bị xóa
            return Restrictions.And(criterion, Restrictions.Or(approved, cancelled));
        }

        /// <summary>
        /// Tính số lượng người và số lượng phòng
        /// </summary>
        /// <param name="cruise"></param>
        /// <param name="date"></param>
        /// <param name="cabins"></param>
        /// <returns></returns>
        public int CountBooked(Cruise cruise, DateTime date, out int cabins)
        {
            // Lấy tất cả booking trong ngày để ra số người, số phòng
            ICriterion criterion = Restrictions.Eq("Cruise", cruise);
            criterion = AddDateExpression(criterion, date);

            criterion = Restrictions.And(criterion, LockCrit());
            //criterion = Restrictions.And(criterion, Restrictions.Eq("Deleted", false));
            //criterion = Restrictions.And(criterion, Restrictions.Eq("Status", StatusType.Approved));

            IList bookings = BookingGetByCriterion(criterion, null, 0, 0);
            if (bookings == null)
            {
                bookings = new List<Booking>();
            }
            int people = 0;
            int rooms = 0;
            foreach (Booking booking in bookings)
            {
                people += booking.Pax;
                rooms += booking.BookingRooms.Count;
            }
            cabins = rooms;
            return people;
        }
        #endregion

        #region -- Obsolete --

        [Obsolete("Không được nâng cấp đầy đủ như các hàm khác, không còn giữ được tính chính xác")]
        public IList RoomGetAvailable(Cruise cruise, BookingRoom bookingRoom, int duration, Booking exception)
        {
            IList lockedRoom = _sailsDao.RoomGetNotAvailable(cruise, bookingRoom.Book.StartDate, duration, exception);

            IList listSameRoom = RoomGetBy_ClassType(cruise, bookingRoom.RoomClass, bookingRoom.RoomType);

            IList result = new ArrayList();
            string notAvailable = string.Empty;
            foreach (Room room in lockedRoom)
            {
                // Nếu không phải shared hoặc chưa chọn thì luôn add
                if (bookingRoom.RoomType.Id != TWIN)
                {
                    notAvailable += "#" + room.Id;
                }
                else
                {
                    // Nếu số khách lớn hơn 1 hoặc room không còn trống chỗ nào thì add
                    if (bookingRoom.VirtualAdult > 1 || !room.IsAvailable)
                    {
                        notAvailable += "#" + room.Id;
                    }
                }
            }
            // Đối với từng room, check xem đã lock chưa và add vào danh sách
            foreach (Room room in listSameRoom)
            {
                if (!notAvailable.Contains("#" + room.Id))
                {
                    result.Add(room);
                }
            }
            return result;
        }

        /// <summary>
        /// Lấy về danh sách phòng và trạng thái phòng theo ngày xác định
        /// </summary>
        /// <param name="cruise"></param>
        /// <param name="date"></param>
        /// <param name="available">Số lượng phòng trống, theo dạng từ điển [RoomTypeId.RoomClassId]</param>
        /// <param name="exception">Không lấy phòng của booking này</param>
        /// <returns></returns>
        public Dictionary<Room, RoomStatus> RoomGetStatus(Cruise cruise, DateTime date, out Dictionary<string, int> available, Booking exception = null)
        {
            var crit = _commonDao.OpenSession().CreateCriteria(typeof(BookingRoom));
            crit.CreateAlias("Book", "booking");

            // Toàn bộ booking có tàu và ngày như đã nhập và đã được approved
            crit.Add(Restrictions.Eq("booking.Cruise", cruise));
            crit.Add(Restrictions.Le("booking.StartDate", date));
            crit.Add(Restrictions.Gt("booking.EndDate", date));
            crit.Add(Restrictions.Eq("booking.Status", StatusType.Approved));

            if (exception != null)
            {
                crit.Add(Restrictions.Not(Restrictions.Eq("booking.Id", exception.Id)));
            }

            var list = crit.List(); // danh sách phòng đã book
            var rooms = RoomGetAll(cruise); //danh sách toàn bộ phòng
            available = new Dictionary<string, int>(); // tạo dữ liệu trắng
            var result = new Dictionary<Room, RoomStatus>();

            // với mỗi phòng mà tàu có thì số phòng trống của loại đó tăng lên 1
            foreach (Room room in rooms)
            {
                string key = string.Format("{0}.{1}", room.RoomType.Id, room.RoomClass.Id);
                if (!available.ContainsKey(key))
                {
                    available.Add(key, 0);
                }
                available[key]++;
                result.Add(room, new RoomStatus() { Room = room });
            }

            // với mỗi phòng mà đã bị book thì số phòng trống của loại đó giảm đi 1
            foreach (BookingRoom bkroom in list)
            {
                string key = string.Format("{0}.{1}", bkroom.RoomType.Id, bkroom.RoomClass.Id);
                available[key]--;

                if (bkroom.Room != null)
                {
                    foreach (Room room in rooms)
                    {
                        if (room.Id == bkroom.Room.Id)
                        {
                            result[room].Status = -1; // nếu đã đặt phòng này thì là phòng bị full
                            result[room].BookingRooms.Add(bkroom);
                            break;
                        }
                    }
                }
            }

            if (exception != null)
                foreach (BookingRoom bkroom in exception.BookingRooms)
                {
                    if (bkroom.Room != null)
                    {
                        foreach (Room room in rooms)
                        {
                            if (room.Id == bkroom.Room.Id)
                            {
                                result[room].Status = 1; // nếu đã đặt phòng này ở booking hiện tại thì status = 1
                                result[room].BookingRooms.Add(bkroom);
                                break;
                            }
                        }
                    }
                }

            return result;
        }

        public Dictionary<Room, RoomStatus> RoomGetStatus(Cruise cruise, DateTime date,
                                                          out Dictionary<string, int> available, out IList bookingrooms)
        {
            var crit = _commonDao.OpenSession().CreateCriteria(typeof(BookingRoom));
            crit.CreateAlias("Book", "booking");

            // Toàn bộ booking có tàu và ngày như đã nhập và đã được approved
            crit.Add(Restrictions.Eq("booking.Cruise", cruise));
            crit.Add(Restrictions.Le("booking.StartDate", date));
            crit.Add(Restrictions.Gt("booking.EndDate", date));
            crit.Add(Restrictions.Eq("booking.Status", StatusType.Approved));

            //if (exception != null)
            //{
            //    crit.Add(Restrictions.Not(Restrictions.Eq("booking.Id", exception.Id)));
            //}

            var list = crit.List(); // danh sách phòng đã book
            bookingrooms = list;
            var rooms = RoomGetAll(cruise); //danh sách toàn bộ phòng
            available = new Dictionary<string, int>(); // tạo dữ liệu trắng
            var result = new Dictionary<Room, RoomStatus>();

            // với mỗi phòng mà tàu có thì số phòng trống của loại đó tăng lên 1
            foreach (Room room in rooms)
            {
                string key = string.Format("{0}.{1}", room.RoomType.Id, room.RoomClass.Id);
                if (!available.ContainsKey(key))
                {
                    available.Add(key, 0);
                }
                available[key]++;
                result.Add(room, new RoomStatus() { Room = room });
            }

            // với mỗi phòng mà đã bị book thì số phòng trống của loại đó giảm đi 1
            foreach (BookingRoom bkroom in list)
            {
                string key = string.Format("{0}.{1}", bkroom.RoomType.Id, bkroom.RoomClass.Id);
                available[key]--;

                if (bkroom.Room != null)
                {
                    foreach (Room room in rooms)
                    {
                        if (room.Id == bkroom.Room.Id)
                        {
                            // nếu khóa thì đánh dấu -1 (không thay đổi được)
                            result[room].Status = -1; // nếu đã đặt phòng này thì là phòng bị full

                            // nếu không thì là 1
                            result[room].Status = 1;

                            result[room].BookingRooms.Add(bkroom);
                            break;
                        }
                    }
                }
            }

            return result;
        }
        #endregion

        #region -- SURVEY --
        public QuestionGroup QuestionGroupGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(QuestionGroup), id) as QuestionGroup;
        }

        public Question QuestionGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(Question), id) as Question;
        }

        public IList QuestionGroupGetAll()
        {
            return _commonDao.GetObjectByCriterion(typeof(QuestionGroup), Restrictions.And(Restrictions.Eq("Deleted", false), Restrictions.Eq("IsInDayboatForm", false)), Order.Asc("Deleted"), Order.Desc("Priority"));
        }

        public IList QuestionGroupGetAllDayboat()
        {
            return _commonDao.GetObjectByCriterion(typeof(QuestionGroup), Restrictions.And(Restrictions.Eq("Deleted", false), Restrictions.Eq("IsInDayboatForm", true)), Order.Asc("Deleted"), Order.Desc("Priority"));
        }

        public AnswerSheet AnswerSheetGetById(int id)
        {
            return (AnswerSheet)_commonDao.GetObjectById(typeof(AnswerSheet), id);
        }

        public AnswerGroup AnswerGroupGetById(int id)
        {
            return (AnswerGroup)_commonDao.GetObjectById(typeof(AnswerGroup), id);
        }

        public IList FeedbackReport(NameValueCollection query)
        {
            ICriterion criterion = Restrictions.Eq("sheet.Deleted", false);
            if (query["group"] == null)
            {
                return new ArrayList();
            }
            QuestionGroup group = QuestionGroupGetById(Convert.ToInt32(query["group"]));
            criterion = Restrictions.And(criterion, Restrictions.Eq("Group", group));

            if (query["from"] != null)
            {
                DateTime from = DateTime.FromOADate(Convert.ToDouble(query["from"]));
                criterion = Restrictions.And(criterion, Restrictions.Ge("sheet.Date", from));
            }

            if (query["to"] != null)
            {
                DateTime from = DateTime.FromOADate(Convert.ToDouble(query["to"]));
                criterion = Restrictions.And(criterion, Restrictions.Le("sheet.Date", from));
            }

            if (query["cruise"] != null)
            {
                Cruise cruise = CruiseGetById(Convert.ToInt32(query["cruise"]));
                criterion = Restrictions.And(criterion, Restrictions.Eq("sheet.Cruise", cruise));
            }
            if (query["guide"] != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("sheet.Guide", query["guide"]));
            }
            if (query["driver"] != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("sheet.Driver", query["driver"]));
            }
            return _sailsDao.FeedbackReport(criterion);
        }

        public int ChoiceReport(NameValueCollection query, Question question, int choice, out int total)
        {
            ICriterion criterion = Restrictions.Eq("sheet.Deleted", false);
            criterion = Restrictions.And(criterion, Restrictions.Eq("Question", question));

            //if (query["group"] == null)
            //{
            //    return new ArrayList();
            //}
            //QuestionGroup group = QuestionGroupGetById(Convert.ToInt32(query["group"]));
            //criterion = Restrictions.And(criterion, Restrictions.Eq("Group", group));

            if (query["from"] != null)
            {
                DateTime from = DateTime.FromOADate(Convert.ToDouble(query["from"]));
                criterion = Restrictions.And(criterion, Restrictions.Ge("sheet.Date", from));
            }

            if (query["to"] != null)
            {
                DateTime from = DateTime.FromOADate(Convert.ToDouble(query["to"]));
                criterion = Restrictions.And(criterion, Restrictions.Le("sheet.Date", from));
            }

            if (query["cruise"] != null)
            {
                Cruise cruise = CruiseGetById(Convert.ToInt32(query["cruise"]));
                criterion = Restrictions.And(criterion, Restrictions.Eq("sheet.Cruise", cruise));
            }
            if (query["guide"] != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("sheet.Guide", query["guide"]));
            }
            if (query["driver"] != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("sheet.Driver", query["driver"]));
            }

            total = _sailsDao.AnswerReport(criterion); // Tổng là chưa có điều kiện choice
            criterion = Restrictions.And(criterion, Restrictions.Eq("Option", choice));
            return _sailsDao.AnswerReport(criterion);
        }

        /// <summary>
        /// Lấy về danh sách các bảng trả lời feedback
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IList FeedbackGet(NameValueCollection query)
        {
            ICriterion criterion = Restrictions.IsNotNull("Id");

            if (query["date"] != null)
            {
                DateTime date = DateTime.FromOADate(Convert.ToDouble(query["date"]));
                criterion = Restrictions.And(criterion, Restrictions.Eq("Date", date));
            }

            return _commonDao.GetObjectByCriterion(typeof(AnswerSheet), criterion);
        }
        #endregion

        #region -- Document --
        public IList DocumentGetCategory()
        {
            ICriterion criterion = Restrictions.IsNull("Parent");
            criterion = Restrictions.And(criterion, Restrictions.Eq("IsCategory", true));
            return _commonDao.GetObjectByCriterion(typeof(DocumentCategory), criterion);
        }

        public IList DocumentGetAll()
        {
            ICriterion criterion = Restrictions.IsNotNull("Parent");
            criterion = Restrictions.And(criterion, Restrictions.Eq("IsCategory", false));
            return _commonDao.GetObjectByCriterion(typeof(DocumentCategory), criterion);
        }

        public DocumentCategory DocumentGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(DocumentCategory), id) as DocumentCategory;
        }
        public IList ChildGetByCategory(int catid)
        {
            ICriterion criterion = Restrictions.Eq("Parent.Id", catid);
            criterion = Restrictions.And(criterion, Restrictions.Eq("IsCategory", true));
            return _commonDao.GetObjectByCriterion(typeof(DocumentCategory), criterion);
        }
        public IList DocumentGetByCategory(int catid)
        {
            ICriterion criterion = Restrictions.Eq("Parent.Id", catid);
            criterion = Restrictions.And(criterion, Restrictions.Eq("IsCategory", false));
            return _commonDao.GetObjectByCriterion(typeof(DocumentCategory), criterion);
        }
        #endregion

        #region -- NEW DAO --
        /// <summary>
        /// Lấy về bóng của booking trong ngày 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public IList<Booking> GetBookingShadow(DateTime date)
        {
            return _sailsDao.GetBookingShadow(date);
        }

        public BookingVoucher GetBookingVoucher(Booking booking, string code)
        {
            var crit = Restrictions.And(Restrictions.Eq("Booking", booking), Restrictions.Eq("Code", code));
            var list = GetObject<BookingVoucher>(crit, 1, 0);

            if (list.Count > 0)
            {
                return list[0];
            }
            return new BookingVoucher() { Booking = booking, Code = code };
        }

        public List<BookingVoucher> GetBookingVoucherByCode(string code)
        {
            var crit = Restrictions.Eq("Code", code);
            var list = GetObject<BookingVoucher>(crit, 0, 0);
            return list?.ToList();
        }

        public T GetObject<T>(int id)
        {
            using (var session = _commonDao.OpenSession())
            {
                return session.Get<T>(id);
            }
        }

        public int CountObjet<T>(ICriterion criterion)
        {
            using (var session = _commonDao.OpenSession())
            {
                ICriteria criteria = session.CreateCriteria(typeof(T));

                criteria.Add(criterion);

                criteria.SetProjection(Projections.RowCount());

                var list = criteria.List();
                if (list.Count > 0 && list[0] != null)
                {
                    return (int)list[0];
                }
                return 0;
            }
        }

        public IList<T> GetObject<T>(ICriterion criterion, int pageSize, int pageIndex, params Order[] orders)
        {
            using (var session = _commonDao.OpenSession())
            {
                ICriteria criteria = session.CreateCriteria(typeof(T));
                if (criterion != null)
                    criteria.Add(criterion);

                if (pageSize > 0)
                {
                    if (pageIndex < 0) pageIndex = 0;
                    criteria.SetFirstResult(pageSize * pageIndex);
                    criteria.SetMaxResults(pageSize);
                }

                foreach (Order order in orders)
                {
                    if (order != null)
                    {
                        criteria.AddOrder(order);
                    }
                }

                return criteria.List<T>();
            }
        }

        #endregion

        public IList AgencyContactGetByTodayBirdthday()
        {
            int month = DateTime.Today.Month;
            int day = DateTime.Today.Day;
            ICriterion criterion = Expression.Sql("DATEPART(d,Birthday) = " + day);
            criterion = Restrictions.And(criterion, Expression.Sql("DATEPART(m,Birthday) = " + month));
            IList agencyContactList = _commonDao.GetObjectByCriterion(typeof(AgencyContact), criterion);
            return agencyContactList;
        }

        #region -- chat ----

        public User GetUserByConnectionId(string connectionId)
        {
            return this._commonDao.OpenSession().QueryOver<User>().Where(u => u.ConnectionId == connectionId).SingleOrDefault();
        }
        public virtual IList<User> GetAllUser()
        {
            return this._commonDao.OpenSession().QueryOver<User>().Where(u => u.IsActive).List();
        }

        public IList<ChatMessage> GetPrivateMessage(string fromid, string toid, int take)
        {
            return this._commonDao.OpenSession().QueryOver<ChatMessage>().Where(m => m.FromUser.Id == Convert.ToInt32(fromid) && m.ToUser.Id == Convert.ToInt32(toid) || (m.FromUser.Id == Convert.ToInt32(toid) && m.ToUser.Id == Convert.ToInt32(fromid)))
                .Where(m => m.ChatGroup == null).OrderBy(m => m.Id).Desc.Take(take).List();
        }
        public IList<ChatMessage> GetPrivateMessage(string fromid, string toid, int takeCounter, int skipCounter)
        {
            var fromUser = _commonDao.GetObjectById(typeof(User), Convert.ToInt32(fromid)) as User;
            var toUser = _commonDao.GetObjectById(typeof(User), Convert.ToInt32(toid)) as User;
            return this._commonDao.OpenSession().QueryOver<ChatMessage>().Where(m => m.FromUser == fromUser && m.ToUser == toUser || (m.FromUser == toUser && m.ToUser == fromUser))
                .Where(m => m.ChatGroup == null).OrderBy(m => m.Id).Desc.Take(takeCounter).Skip(skipCounter).List();
        }
        public IList<ChatGroup> GetAllGroup(int userId)
        {
            ChatGroupUser chatGroupUser = null;
            return this._commonDao.OpenSession().QueryOver<ChatGroup>().Left.JoinAlias(c => c.ChatGroupUsers, () => chatGroupUser)
                .Where(c => chatGroupUser.User.Id == userId).OrderBy(m => m.Id).Desc.List();
        }

        public IList<ChatMessage> GetGroupMessage(int userId, int groupId, int take)
        {
            var userGroup = _commonDao.OpenSession().QueryOver<ChatGroupUser>()
                .Where(u => u.User.Id == userId && u.ChatGroup.Id == groupId).SingleOrDefault();
            if (userGroup != null)
            {
                return this._commonDao.OpenSession().QueryOver<ChatMessage>().Where(m => m.ChatGroup.Id == Convert.ToInt32(groupId))
                    .OrderBy(m => m.Id).Desc.Take(take).List();
            }
            return new List<ChatMessage>();
        }

        public IList<ChatMessage> GetGroupMessage(int userId, int groupId, int takeCounter, int skipCounter)
        {
            var userGroup = _commonDao.OpenSession().QueryOver<ChatGroupUser>()
                .Where(u => u.User.Id == userId && u.ChatGroup.Id == groupId).SingleOrDefault();
            if (userGroup != null)
            {
                return this._commonDao.OpenSession().QueryOver<ChatMessage>()
                    .Where(m => m.ChatGroup.Id == Convert.ToInt32(groupId))
                    .OrderBy(m => m.Id).Desc.Take(takeCounter).Skip(skipCounter).List();
            }
            return new List<ChatMessage>();
        }

        public bool CheckExistUserInGroup(User user, int groupId)
        {
            var userGroup = _commonDao.OpenSession().QueryOver<ChatGroupUser>()
                .Where(u => u.User.Id == user.Id && u.ChatGroup.Id == groupId).SingleOrDefault();
            return userGroup != null;
        }

        public void OutGroup(int userId, int groupId)
        {
            var userGroup = _commonDao.OpenSession().QueryOver<ChatGroupUser>()
                .Where(u => u.User.Id == userId && u.ChatGroup.Id == groupId).SingleOrDefault();
            if (userGroup != null)
            {
                this._commonDao.DeleteObject(userGroup);
            }
        }

        public IList<ChatGroupUser> GetGroupUser(int groupId)
        {
            return this._commonDao.OpenSession().QueryOver<ChatGroupUser>().Where(c => c.ChatGroup.Id == groupId).OrderBy(m => m.Id).Desc.List();
        }

        public bool CheckUserExistGroup(int userId, int groupId)
        {
            var userGroup = _commonDao.OpenSession().QueryOver<ChatGroupUser>()
                .Where(u => u.User.Id == userId && u.ChatGroup.Id == groupId).SingleOrDefault();
            return userGroup != null && userGroup.Id > 0;
        }

        #endregion

        #region -- Report Debt Receivables --
        /// <summary>
        /// lấy các booking có nợ phải thu
        /// </summary>
        /// <param name="to"></param>
        /// <param name="agencyId"></param>
        /// <returns></returns>
        public IList<Booking> GetBookingDebtReceivables(DateTime to, int agencyId)
        {
            var query = _commonDao.OpenSession().QueryOver<Booking>().Where(x => x.Deleted == false && x.StartDate <= to);
            query = query.Where(Restrictions.Or(Restrictions.Where<Booking>(x => x.Status == StatusType.Approved && x.Paid != x.Total && x.Total != 0), Restrictions.Where<Booking>(x => x.Status == StatusType.Cancelled && x.CancelPay > 0)));
            query = query.Where(x => x.IsPaid != true);
            //query = query.Where(x => x.Status == StatusType.Approved && x.Paid < x.Total || x.Status == StatusType.Cancelled && x.Paid < x.CancelPay);

            Agency agencyAlias = null;
            query = query.JoinAlias(x => x.Agency, () => agencyAlias);
            if (agencyId != -1)
            {
                query = query.Where(x => agencyAlias.Id == agencyId);
            }
            return query.List<Booking>();
        }
        //public IList<DebtReceivable> GetReportDebtReceivables(DateTime to, int agencyId)
        //{
        //    var query = _commonDao.OpenSession().QueryOver<Booking>().Where(x => x.StartDate <= to);
        //    query = query.Where(x => x.IsPaid != true);
        //    query = query.Where(x => x.Status == StatusType.Approved && x.Paid < x.Total || x.Status == StatusType.Cancelled && x.Paid < x.CancelPay);
        //    Agency agencyAlias = null;
        //    query = query.JoinAlias(x => x.Agency, () => agencyAlias);
        //    if (agencyId != -1)
        //    {
        //        query = query.Where(x => agencyAlias.Id == agencyId);
        //    }
        //    return query.List<Booking>()
        //        .GroupBy(x => x.Agency).Select(
        //            g => new DebtReceivable()
        //            {
        //                Agency = g.First().Agency,
        //                TotalReceivable = g.Sum(x => (x.Total - x.Paid) * x.CurrencyRate - x.PaidBase),
        //            }).OrderByDescending(d => d.TotalReceivable).ToList();
        //}

        #endregion

        #region --INVENTORY--
        public Role RoleGetByName(string name)
        {
            return _commonDao.OpenSession().QueryOver<Role>().Where(x => x.Name == name).SingleOrDefault();
        }
        public IList<Agency> AgencyGetAllByRole(Role role)
        {
            return _commonDao.OpenSession().QueryOver<Agency>().Where(x => x.Deleted == false).Where(x => x.Role == role).List();
        }
        public IvCategory IvCategoryGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(IvCategory), id) as IvCategory;
        }
        public IvUnit IvUnitGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(IvUnit), id) as IvUnit;
        }
        public IvStorage IvStorageGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(IvStorage), id) as IvStorage;
        }
        public IvProduct IvProductGetById(int id)
        {
            return _commonDao.GetObjectById(typeof(IvProduct), id) as IvProduct;
        }
        /// <summary>
        /// đếm phiếu xuất theo mã
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public int IvExportCheckByCode(string code)
        {
            int count = 0;
            ICriterion criterion = Restrictions.Eq("Code", code);
            count = _commonDao.CountObjectByCriterion(typeof(IvExport), criterion);
            return count;
        }
        /// <summary>
        /// đếm phiếu thu theo mã
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public int IvImportCheckByCode(string code)
        {
            int count = 0;
            ICriterion criterion = Restrictions.Eq("Code", code);
            count = _commonDao.CountObjectByCriterion(typeof(IvImport), criterion);
            return count;
        }
        /// <summary>
        /// lấy sản phẩm theo mã code, kho
        /// </summary>
        /// <param name="code"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        public IvProduct IvProductGetByCode(string code, IvStorage storage)
        {
            ICriterion criterion = Restrictions.Eq("Deleted", false);
            if (!string.IsNullOrWhiteSpace(code))
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("Code", code));
                //if (storage != null)
                //{
                //    criterion = Restrictions.And(criterion, Restrictions.Eq("Storage.Id", storage.Id));
                //}
                IList listProduct = _commonDao.GetObjectByCriterion(typeof(IvProduct), criterion,
                    Order.Desc("CreatedDate"));

                if (listProduct != null && listProduct.Count > 0)
                {
                    return (IvProduct)listProduct[0];
                }
            }
            return null;
        }
        /// <summary>
        /// lấy cấu hình cảnh báo hết sản phẩm theo sp, kho
        /// </summary>
        /// <param name="product"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        public IvProductWarning GetProductWarning(IvProduct product, IvStorage storage)
        {
            return _commonDao.OpenSession().QueryOver<IvProductWarning>()
                .Where(p => p.Product.Id == product.Id && p.Storage.Id == storage.Id).List().FirstOrDefault();
        }
        /// <summary>
        /// lấy giá sản phẩm
        /// </summary>
        /// <param name="product">sản phẩm</param>
        /// <param name="unit">đơn vị tính</param>
        /// <param name="storage">kho</param>
        /// <returns></returns>
        public IvProductPrice GetProductPrice(IvProduct product, IvUnit unit, IvStorage storage)
        {
            return _commonDao.OpenSession().QueryOver<IvProductPrice>()
                .Where(p => p.Product.Id == product.Id && p.Unit.Id == unit.Id && p.Storage.Id == storage.Id).List().FirstOrDefault();
        }
        /// <summary>
        /// lấy toàn bộ cấu hình cảnh báo hết của sản phẩm theo kho
        /// </summary>
        /// <param name="storage"></param>
        /// <returns></returns>
        public IList<IvProductWarning> GetProductWarningByStorage(IvStorage storage)
        {
            return _commonDao.OpenSession().QueryOver<IvProductWarning>()
                .Where(p => p.Storage.Id == storage.Id).List();
        }
        /// <summary>
        /// lấy sản phẩm theo điều kiện, phân trang
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize">size trang</param>
        /// <param name="pageIndex">trang thứ </param>
        /// <param name="total">tổng tìm được</param>
        /// <returns></returns>
        public IList IvProductGetByQuery(NameValueCollection query, int pageSize, int pageIndex, out int total)
        {
            ICriterion criterion = Restrictions.Eq("Deleted", false);
            if (!string.IsNullOrWhiteSpace(query["name"]))
                criterion = Restrictions.And(criterion, Restrictions.Like("Name", query["name"], MatchMode.Anywhere));
            if (!string.IsNullOrWhiteSpace(query["cateId"]))
                criterion = Restrictions.And(criterion, Restrictions.Eq("Category.Id", Convert.ToInt32(query["cateId"])));

            total = _commonDao.CountObjectByCriterion(typeof(IvProduct), criterion);
            return _commonDao.GetObjectByCriterionPaged(typeof(IvProduct), criterion, pageIndex, pageSize);
        }
        /// <summary>
        /// lấy kho theo điều kiện
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IList IvStorageGetByQuery(NameValueCollection query)
        {
            var storages = _commonDao.OpenSession().QueryOver<IvStorage>().List<IvStorage>();
            if (!string.IsNullOrWhiteSpace(query["name"]))
                storages = storages.Where(c => c.Name.ToLower().Contains(query["name"].ToLower())).ToList();
            if (!string.IsNullOrWhiteSpace(query["parentId"]))
                storages = storages.Where(c => c.Parent != null && c.Parent.Id == Convert.ToInt32(query["parentId"])).ToList();
            foreach (IvStorage storage in storages)
            {
                storage.NameTree = storage.Name;
            }
            return storages.ToList();
        }
        /// <summary>
        /// lấy toàn bộ kho con theo kho cha
        /// </summary>
        /// <param name="currentCategory"></param>
        /// <returns></returns>
        public IList IvStorageGetAll(IvStorage currentCategory)
        {
            // lấy danh sách thành phần con
            var storages = _commonDao.OpenSession().QueryOver<IvStorage>().List<IvStorage>();
            if (currentCategory != null && currentCategory.Id > 0) storages.Remove(storages.FirstOrDefault(c => c.Id == currentCategory.Id));
            var dtRoot = new List<IvStorage>();
            foreach (IvStorage storage in storages)
            {
                if (storage.Parent == null)
                {
                    storage.NameTree = storage.Name;
                    dtRoot.Add(storage);
                    LoadChildStorageTree(dtRoot, storage, storages, 0);
                }
            }
            return dtRoot;
        }
        /// <summary>
        /// lấy toàn bộ kho của tàu
        /// </summary>
        /// <param name="cruiseId"></param>
        /// <returns></returns>
        public IList IvStorageGetAll(int cruiseId)
        {
            // lấy danh sách thành phần con
            var storages = _commonDao.OpenSession().QueryOver<IvStorage>().Where(s => s.Cruise.Id == cruiseId).List<IvStorage>();
            var dtRoot = new List<IvStorage>();
            foreach (IvStorage storage in storages)
            {
                if (storage.Parent == null)
                {
                    storage.NameTree = storage.Name;
                    dtRoot.Add(storage);
                    LoadChildStorageTree(dtRoot, storage, storages, 0);
                }
            }
            return dtRoot;
        }
        /// <summary>
        /// lấy toàn bộ kho theo phân quyền user
        /// </summary>
        /// <param name="userIdentity"></param>
        /// <returns></returns>
        public IList IvStorageGetByUser(User userIdentity)
        {
            if (userIdentity.HasPermission(AccessLevel.Administrator))
                return IvStorageGetAll(null);
            else
            {
                var roleCruises = _commonDao.GetObjectByCriterion(typeof(IvRoleCruise), Restrictions.Or(Restrictions.Eq("User.Id", userIdentity.Id), Restrictions.In("Role", userIdentity.Roles)));

                var ids = new List<int>();
                foreach (IvRoleCruise roleCruise in roleCruises)
                {
                    ids.Add(roleCruise.Cruise.Id);
                }
                return _commonDao.GetObjectByCriterion(typeof(IvStorage), Restrictions.In("Cruise.Id", ids));
            }
        }
        /// <summary>
        /// lấy kho con theo kho cha
        /// </summary>
        /// <param name="dtRoot"></param>
        /// <param name="newsCategory"></param>
        /// <param name="storages"></param>
        /// <param name="level"></param>
        private void LoadChildStorageTree(List<IvStorage> dtRoot, IvStorage newsCategory, IList<IvStorage> storages,
            int level)
        {
            int lv = level + 1;
            if (dtRoot != null && dtRoot.Count > 0)
            {
                var list = storages.Where(page => page.Parent != null && page.Parent.Id == newsCategory.Id).ToList();
                if (list.Count <= 0) return;
                foreach (IvStorage storage in list)
                {
                    storage.NameTree = MiscUtility.StringIndent(lv) + "|_ " + storage.Name;
                    dtRoot.Add(storage);
                    LoadChildStorageTree(dtRoot, storage, storages, lv);
                }
            }
        }
        /// <summary>
        /// lấy danh mục sản phẩm
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IList IvCategoryGetByQuery(NameValueCollection query)
        {
            var allCategory = _commonDao.OpenSession().QueryOver<IvCategory>().List<IvCategory>();
            if (!string.IsNullOrWhiteSpace(query["name"]))
                allCategory = allCategory.Where(c => c.Name.ToLower().Contains(query["name"].ToLower())).ToList();
            if (!string.IsNullOrWhiteSpace(query["parentId"]))
                allCategory = allCategory.Where(c => c.Parent != null && c.Parent.Id == Convert.ToInt32(query["parentId"])).ToList();
            foreach (IvCategory category in allCategory)
            {
                category.NameTree = category.Name;
            }
            return allCategory.ToList();
        }
        /// <summary>
        /// lấy toàn bộ danh mục sp theo danh mục cha
        /// </summary>
        /// <param name="currentCategory"></param>
        /// <returns></returns>
        public IList IvCategoryGetAll(IvCategory currentCategory)
        {
            // lấy danh sách thành phần con
            var allCategory = _commonDao.OpenSession().QueryOver<IvCategory>().List<IvCategory>();
            if (currentCategory != null && currentCategory.Id > 0) allCategory.Remove(allCategory.FirstOrDefault(c => c.Id == currentCategory.Id));
            var dtRoot = new List<IvCategory>();
            foreach (IvCategory catagory in allCategory)
            {
                if (catagory.Parent == null)
                {
                    catagory.NameTree = catagory.Name;
                    dtRoot.Add(catagory);
                    LoadChildCategoryTree(dtRoot, catagory, allCategory, 0);
                }
            }
            return dtRoot;
        }
        /// <summary>
        /// lấy toàn bộ danh mục con theo danh mục cha
        /// </summary>
        /// <param name="dtRoot"></param>
        /// <param name="newsCategory"></param>
        /// <param name="allCatagories"></param>
        /// <param name="level"></param>
        private void LoadChildCategoryTree(List<IvCategory> dtRoot, IvCategory newsCategory, IList<IvCategory> allCatagories,
            int level)
        {
            int lv = level + 1;
            if (dtRoot != null && dtRoot.Count > 0)
            {
                var list = allCatagories.Where(page => page.Parent != null && page.Parent.Id == newsCategory.Id).ToList();
                if (list.Count <= 0) return;
                foreach (IvCategory category in list)
                {
                    category.NameTree = MiscUtility.StringIndent(lv) + "|_ " + category.Name;
                    dtRoot.Add(category);
                    LoadChildCategoryTree(dtRoot, category, allCatagories, lv);
                }
            }
        }
        /// <summary>
        /// lấy toàn bộ đơn vị tính
        /// </summary>
        /// <returns></returns>
        public IList<IvUnit> IvUnitGetAll()
        {
            // lấy danh sách thành phần con
            return _commonDao.OpenSession().QueryOver<IvUnit>().List<IvUnit>();
        }
        /// <summary>
        /// lấy toàn bộ đơn vị cấp con tính theo đơn vị cha
        /// </summary>
        /// <param name="currentUnit"></param>
        /// <returns></returns>
        public IList IvUnitGetAll(IvUnit currentUnit)
        {
            // lấy danh sách thành phần con
            var units = _commonDao.OpenSession().QueryOver<IvUnit>().List<IvUnit>();
            if (currentUnit != null && currentUnit.Id > 0) units.Remove(units.FirstOrDefault(c => c.Id == currentUnit.Id));
            var dtRoot = new List<IvUnit>();
            foreach (IvUnit unit in units)
            {
                if (unit.Parent == null)
                {
                    unit.NameTree = unit.Name;
                    dtRoot.Add(unit);
                    LoadChildUnitTree(dtRoot, unit, units, 0);
                }
            }
            return dtRoot;
        }
        /// <summary>
        /// lấy theo cây đơn vị tính
        /// </summary>
        /// <param name="dtRoot"></param>
        /// <param name="newsCategory"></param>
        /// <param name="units"></param>
        /// <param name="level"></param>
        private void LoadChildUnitTree(List<IvUnit> dtRoot, IvUnit newsCategory, IList<IvUnit> units,
            int level)
        {
            int lv = level + 1;
            if (dtRoot != null && dtRoot.Count > 0)
            {
                var list = units.Where(page => page.Parent != null && page.Parent.Id == newsCategory.Id).ToList();
                if (list.Count <= 0) return;
                foreach (IvUnit unit in list)
                {
                    unit.NameTree = MiscUtility.StringIndent(lv) + "|_ " + unit.Name;
                    dtRoot.Add(unit);
                    LoadChildUnitTree(dtRoot, unit, units, lv);
                }
            }
        }
        /// <summary>
        /// lấy đơn vị tính theo đơn vị cha
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public IList<IvUnit> IvUnitGetAllParent(IvUnit parent)
        {
            var query = _commonDao.OpenSession().QueryOver<IvUnit>().Where(i => i.Parent == null);
            if (parent != null)
                query = query.Where(i => i.Id != parent.Id);
            return query.List<IvUnit>();
        }
        /// <summary>
        /// lấy sản phẩm theo điều kiện tìm kiếm, phân trang
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IList IvProductGetByQueryString(NameValueCollection query, User user, int pageSize, int pageIndex, out int count)
        {
            ICriterion criterion = Restrictions.Eq("Deleted", false);
            if (!string.IsNullOrEmpty(query["name"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Name", query["name"], MatchMode.Anywhere));
            }
            if (!string.IsNullOrEmpty(query["code"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Code", query["code"], MatchMode.Anywhere));
            }
            if (!string.IsNullOrEmpty(query["cateId"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("Category.Id", Convert.ToInt32(query["cateId"])));
            }
            Order order = RepeaterOrder.GetOrderFromQueryString(query);
            if (order == null)
            {
                order = Order.Desc("CreatedDate");
            }

            count = IvProductCountByCriterion(criterion, 0);
            return IvProductGetByCriterionPaged(criterion, pageSize, pageIndex, 0, order);
        }
        /// <summary>
        /// lấy sản phẩm trong kho theo mã, kho
        /// </summary>
        /// <param name="code"></param>
        /// <param name="storageId"></param>
        /// <returns></returns>
        public IvInStock GetProductInStockByCode(string code, string storageId)
        {
            ICriterion criterion = Restrictions.Gt("Id", 0);
            if (!string.IsNullOrEmpty(code))
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Code", code, MatchMode.Anywhere));
            }
            if (!string.IsNullOrEmpty(storageId))
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("StorageId", Convert.ToInt32(storageId)));
            }
            var list = _commonDao.GetObjectByCriterion(typeof(IvInStock), criterion);
            if (list != null && list.Count > 0)
                return list[0] as IvInStock;
            return null;
        }
        /// <summary>
        /// lấy sản phẩm trong kho
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IList GetProductInStock(NameValueCollection query, User user, int pageSize, int pageIndex, out int count)
        {
            ICriterion criterion = Restrictions.Gt("Id", 0);
            if (!string.IsNullOrEmpty(query["name"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Name", query["name"], MatchMode.Anywhere));
            }
            if (!string.IsNullOrEmpty(query["code"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Code", query["code"], MatchMode.Anywhere));
            }
            if (!string.IsNullOrEmpty(query["cateId"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("CategoryId", Convert.ToInt32(query["cateId"])));
            }
            if (!string.IsNullOrEmpty(query["storageId"]))
            {
                var storageId = Convert.ToInt32(query["storageId"]);
                criterion = Restrictions.And(criterion, Restrictions.Eq("StorageId", storageId));
            }
            if (!user.HasPermission(AccessLevel.Administrator))
            {
                var list = IvStorageGetByUser(user);
                var storages = new List<int>();
                foreach (IvStorage storage in list)
                {
                    storages.Add(storage.Id);
                }
                criterion = Restrictions.And(criterion, Restrictions.In("StorageId", storages));
            }
            count = _commonDao.CountObjectByCriterion(typeof(IvInStock), criterion);
            return _commonDao.GetObjectByCriterionPaged(typeof(IvInStock), criterion, pageIndex, pageSize);
        }
        /// <summary>
        /// lấy sản phẩm trong kho
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IList GetSelectProductInStock(NameValueCollection query, User user, int pageSize, int pageIndex, out int count)
        {
            ICriterion criterion = Restrictions.Eq("IsTool", false);
            if (!string.IsNullOrEmpty(query["name"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Name", query["name"], MatchMode.Anywhere));
            }
            if (!string.IsNullOrEmpty(query["code"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Code", query["code"], MatchMode.Anywhere));
            }
            if (!string.IsNullOrEmpty(query["cateId"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("CategoryId", Convert.ToInt32(query["cateId"])));
            }
            if (!string.IsNullOrEmpty(query["storageId"]))
            {
                var storageId = Convert.ToInt32(query["storageId"]);
                criterion = Restrictions.And(criterion, Restrictions.Eq("StorageId", storageId));
            }
            if (!user.HasPermission(AccessLevel.Administrator))
            {
                var list = IvStorageGetByUser(user);
                var storages = new List<int>();
                foreach (IvStorage storage in list)
                {
                    storages.Add(storage.Id);
                }
                criterion = Restrictions.And(criterion, Restrictions.In("StorageId", storages));
            }
            count = _commonDao.CountObjectByCriterion(typeof(IvInStock), criterion);
            return _commonDao.GetObjectByCriterionPaged(typeof(IvInStock), criterion, pageIndex, pageSize);
        }
        /// <summary>
        /// đếm số sản phẩm
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public int IvProductCountByCriterion(ICriterion criterion, int groupId)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProduct));

            //if (groupId > 0)
            //{
            //    criteria.CreateAlias("Group", "group", NHibernate.SqlCommand.JoinType.LeftOuterJoin);
            //    criteria.Add(Restrictions.Eq("group.Id", groupId));
            //}
            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.RowCount());

            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (int)list[0];
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// lấy sản phẩm 
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="groupId"></param>
        /// <param name="orders"></param>
        /// <returns></returns>
        public IList IvProductGetByCriterionPaged(ICriterion criterion, int pageSize, int pageIndex, int groupId, params Order[] orders)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProduct));

            ////if (groupId > 0)
            ////{
            ////    criteria.CreateAlias("Group", "group", NHibernate.SqlCommand.JoinType.LeftOuterJoin);
            ////    criteria.Add(Restrictions.Eq("group.Id", groupId));
            ////}

            if (criterion != null)
            {
                criteria.Add(criterion);
            }
            foreach (Order order in orders)
            {
                if (order != null)
                {
                    criteria.AddOrder(order);
                }
            }

            if (pageSize > 0)
            {
                if (pageIndex <= 0) pageIndex = 0;
                criteria.SetFirstResult(pageSize * pageIndex);
                criteria.SetMaxResults(pageSize);
            }



            return criteria.List();
        }
        /// <summary>
        /// lấy cấu hình phân quyền theo tàu
        /// </summary>
        /// <param name="cruiseId"></param>
        /// <returns></returns>
        public IList<IvRoleCruise> IvRoleCruiseGetByCruiseId(int cruiseId)
        {
            return _commonDao.OpenSession().QueryOver<IvRoleCruise>().Where(c => c.Cruise.Id == cruiseId).List();
        }
        /// <summary>
        /// lấy phiếu thu theo điều kiện
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="count"></param>
        /// <param name="sum"></param>
        /// <returns></returns>
        public IList IvImportGetByQueryString(NameValueCollection query, User user, int pageSize, int pageIndex, out int count,
            out double sum)
        {
            ICriterion criterion = Restrictions.Eq("Deleted", false);

            if (string.IsNullOrEmpty(query["name"]) && string.IsNullOrEmpty(query["code"]) &&
                string.IsNullOrEmpty(query["time"]))
            {
                //DateTime firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                //DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);

                //criterion = Restrictions.And(criterion, Restrictions.Ge("ImportDate", firstDay));
                //criterion = Restrictions.And(criterion, Restrictions.Le("ImportDate", lastDay));
            }
            if (!string.IsNullOrEmpty(query["storageId"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("Storage.Id", Convert.ToInt32(query["storageId"])));
            }
            if (!string.IsNullOrEmpty(query["name"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Name", query["name"], MatchMode.Anywhere));
            }
            if (!string.IsNullOrEmpty(query["code"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Code", query["code"], MatchMode.Anywhere));
            }
            if (!string.IsNullOrEmpty(query["time"]))
            {
                double time = Convert.ToDouble(query["time"]);
                DateTime FtimeConvert = DateTime.FromOADate(time);
                DateTime LtimeConvert = FtimeConvert.AddMonths(1).AddDays(-1);

                criterion = Restrictions.And(criterion, Restrictions.Ge("ImportDate", FtimeConvert));
                criterion = Restrictions.And(criterion, Restrictions.Le("ImportDate", LtimeConvert));
            }

            if (!string.IsNullOrEmpty(query["fromDate"]))
            {
                double time = Convert.ToDouble(query["fromDate"]);
                DateTime ffromDate = DateTime.FromOADate(time);
                DateTime ltimeConvert = ffromDate.AddDays(-1);
                criterion = Restrictions.And(criterion, Restrictions.Gt("ImportDate", ltimeConvert));
            }
            if (!string.IsNullOrEmpty(query["toDate"]))
            {
                double time = Convert.ToDouble(query["toDate"]);
                DateTime ffromDate = DateTime.FromOADate(time);
                DateTime ltimeConvert = ffromDate.AddDays(1);
                criterion = Restrictions.And(criterion, Restrictions.Lt("ImportDate", ltimeConvert));
            }
            AddStorageCriterion(ref criterion, user, query);
            Order order = RepeaterOrder.GetOrderFromQueryString(query);
            if (order == null)
            {
                order = Order.Desc("ImportDate");
            }

            count = IvImportCountByCriterion(criterion);
            sum = IvImportTotalByCriterion(criterion);
            return IvImportGetByCriterionPaged(criterion, pageSize, pageIndex, order);
        }
        /// <summary>
        /// thêm điều kiện lấy kho theo phân quyền
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="user"></param>
        /// <param name="query"></param>
        private void AddStorageCriterion(ref ICriterion criterion, User user, NameValueCollection query)
        {
            if (!string.IsNullOrEmpty(query["storageId"]))
            {
                var storageId = Convert.ToInt32(query["storageId"]);
                criterion = Restrictions.And(criterion, Restrictions.Eq("Storage.Id", storageId));
            }
            if (!user.HasPermission(AccessLevel.Administrator))
            {
                var list = IvStorageGetByUser(user);
                var storages = new List<int>();
                foreach (IvStorage storage in list)
                {
                    storages.Add(storage.Id);
                }
                criterion = Restrictions.And(criterion, Restrictions.In("Storage.Id", storages));
            }
        }
        #region ---SaleImport---
        /// <summary>
        /// đếm số phiếu thu
        /// </summary>
        /// <param name="criterion"></param>
        /// <returns></returns>
        public double IvImportTotalByCriterion(ICriterion criterion)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvImport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.Sum("Total"));
            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (double)list[0];
            }
            else
            {
                return 0;
            }

        }
        /// <summary>
        /// đếm số phiếu thu
        /// </summary>
        /// <param name="criterion"></param>
        /// <returns></returns>
        public int IvImportCountByCriterion(ICriterion criterion)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvImport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.RowCount());

            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (int)list[0];
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// lấy số phiếu thu
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="orders"></param>
        /// <returns></returns>
        public IList IvImportGetByCriterionPaged(ICriterion criterion, int pageSize, int pageIndex, params Order[] orders)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvImport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }
            foreach (Order order in orders)
            {
                if (order != null)
                {
                    criteria.AddOrder(order);
                }
            }

            if (pageSize > 0)
            {
                if (pageIndex <= 0) pageIndex = 0;
                criteria.SetFirstResult(pageSize * pageIndex);
                criteria.SetMaxResults(pageSize);
            }
            return criteria.List();
        }
        /// <summary>
        /// đếm số tiền nhập của phiếu thu theo tháng
        /// </summary>
        /// <param name="criterion"></param>
        /// <returns></returns>
        public double IvImportSumTotalByMonth(ICriterion criterion)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvImport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.Sum("Total"));
            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (double)list[0];
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// đếm số phiếu thu theo ngày
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public int CountImportByDateTime(NameValueCollection query, User user, DateTime date)
        {
            ICriterion criterion = Restrictions.Eq("ImportDate", date);
            AddStorageCriterion(ref criterion, user, query);

            return _commonDao.CountObjectByCriterion(typeof(IvImport), criterion);
        }
        /// <summary>
        /// lấy số sản phẩm nhập theo điều kiện tìm kiếm
        /// </summary>
        /// <param name="user"></param>
        /// <param name="import"></param>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IList IvProductImportGetByImport(User user, IvImport import, NameValueCollection query, int pageSize,
            int pageIndex, out int count)
        {
            ICriterion criterion = Restrictions.Eq("Import", import);
            AddStorageCriterion(ref criterion, user, query);
            Order order = RepeaterOrder.GetOrderFromQueryString(query);
            if (order == null)
            {
                order = Order.Desc("Id");
            }

            count = IvProductImportCountByCriterion(criterion);
            return IvProductImportGetByCriterionPaged(criterion, pageSize, pageIndex, order);
        }
        /// <summary>
        /// đếm số phiếu nhập theo mã phiếu
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public int CountImportGetByCode(NameValueCollection query, User user, string code)
        {
            ICriterion criterion = Restrictions.Eq("Code", code);
            AddStorageCriterion(ref criterion, user, query);

            return _commonDao.CountObjectByCriterion(typeof(IvImport), criterion);
        }
        /// <summary>
        /// đếm số phiếu thu theo tháng
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        public double ImportTotalGetByMonth(NameValueCollection query, User user, int month)
        {
            DateTime firstDay = new DateTime(DateTime.Today.Year, month, 1);
            DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);

            ICriterion criterion = Restrictions.Ge("ImportDate", firstDay);
            criterion = Restrictions.And(criterion, Restrictions.Le("ImportDate", lastDay));
            AddStorageCriterion(ref criterion, user, query);

            return IvImportTotalByCriterion(criterion);
        }
        /// <summary>
        /// đếm số tiền phiếu thu theo ngày
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        public double ImportTotalGetByDay(NameValueCollection query, User user, DateTime day)
        {
            ICriterion criterion = Restrictions.Eq("ImportDate", day);
            AddStorageCriterion(ref criterion, user, query);
            return IvImportTotalByCriterion(criterion);
        }
        /// <summary>
        /// đếm số tiền phiếu thu theo tháng
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        public double ImportSumImcomeGetByMonth(NameValueCollection query, User user, double day)
        {
            DateTime time = DateTime.FromOADate(day);
            DateTime lastTime = time.AddMonths(1).AddDays(-1);
            ICriterion criterion = Restrictions.Ge("ImportDate", time);
            AddStorageCriterion(ref criterion, user, query);

            criterion = Restrictions.And(criterion, Restrictions.Le("ImportDate", lastTime));

            return IvImportSumTotalByMonth(criterion);
        }
        #endregion

        #region ---SaleExport---
        /// <summary>
        /// đếm số phiếu xuất 
        /// </summary>
        /// <param name="criterion"></param>
        /// <returns></returns>
        public int IvExportCountByCriterion(ICriterion criterion)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvExport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.RowCount());
            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (int)list[0];
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="orders"></param>
        /// <returns></returns>
        public IList SaleExportGetByCriterionPaged(ICriterion criterion, int pageSize, int pageIndex, params Order[] orders)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvExport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }
            foreach (Order order in orders)
            {
                if (order != null)
                {
                    criteria.AddOrder(order);
                }
            }

            if (pageSize > 0)
            {
                if (pageIndex <= 0) pageIndex = 0;
                criteria.SetFirstResult(pageSize * pageIndex);
                criteria.SetMaxResults(pageSize);
            }
            return criteria.List();
        }
        /// <summary>
        /// tính tổng giá tiền phiếu xuất
        /// </summary>
        /// <param name="criterion"></param>
        /// <returns></returns>
        public double IvExportTotalByCriterion(ICriterion criterion)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvExport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.Sum("Total"));
            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (double)list[0];
            }
            else
            {
                return 0;
            }

        }
        /// <summary>
        /// tính tổng giá tiền phiếu xuất theo tháng
        /// </summary>
        /// <param name="criterion"></param>
        /// <returns></returns>
        public double IvExportSumTotalByMonth(ICriterion criterion)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvExport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.Sum("Total"));
            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (double)list[0];
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// tính tổng giá tiền phiếu xuất theo khoảng thời gian
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="firstDate"></param>
        /// <param name="lastDate"></param>
        /// <returns></returns>
        public double IvExportTotalByDateTime(ICriterion criterion, double firstDate, double lastDate)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvExport));

            ICriterion dateCrit = null;
            if (firstDate > 0)
            {
                DateTime fromDate = DateTime.FromOADate(firstDate);
                dateCrit = Restrictions.Ge("ExportDate", fromDate);
            }
            if (lastDate > 0)
            {
                DateTime toDate = DateTime.FromOADate(lastDate);
                if (dateCrit != null)
                {
                    dateCrit = Restrictions.And(dateCrit, Restrictions.Le("ExportDate", toDate));
                }
                else
                {
                    dateCrit = Restrictions.Le("ExportDate", toDate);
                }
            }

            if (criterion != null && dateCrit != null)
            {
                criterion = Restrictions.And(criterion, dateCrit);
            }
            else if (dateCrit != null)
            {
                criterion = dateCrit;
            }

            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.Sum("Total"));
            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (double)list[0];
            }
            else
            {
                return 0;
            }
        }


        #endregion

        #region ---ProductImport---
        /// <summary>
        /// đếm số phiếu thu 
        /// </summary>
        /// <param name="criterion"></param>
        /// <returns></returns>
        public int IvProductImportCountByCriterion(ICriterion criterion)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductImport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.RowCount());
            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (int)list[0];
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// lấy sản phẩm khi xuất phiếu
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="orders"></param>
        /// <returns></returns>
        public IList IvProductImportGetByCriterionPaged(ICriterion criterion, int pageSize, int pageIndex, params Order[] orders)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductImport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }
            foreach (Order order in orders)
            {
                if (order != null)
                {
                    criteria.AddOrder(order);
                }
            }

            if (pageSize > 0)
            {
                if (pageIndex <= 0) pageIndex = 0;
                criteria.SetFirstResult(pageSize * pageIndex);
                criteria.SetMaxResults(pageSize);
            }
            return criteria.List();
        }
        /// <summary>
        /// tổng số sản phẩm đã nhập
        /// </summary>
        /// <param name="criterion"></param>
        /// <returns></returns>
        public int TotalProductImportByProductId(ICriterion criterion)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductImport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.Sum("Quantity"));
            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (int)list[0];
            }
            else
            {
                return 0;
            }

        }
        /// <summary>
        /// lấy báo cáo các sản phẩm nhập
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="firstDate"></param>
        /// <param name="lastDate"></param>
        /// <param name="sumQuantity"></param>
        /// <param name="sumTotal"></param>
        /// <param name="orders"></param>
        /// <returns></returns>
        public IList ImportReportGetByCriterionPaged(ICriterion criterion, int pageSize, int pageIndex, DateTime firstDate, DateTime lastDate, out int sumQuantity, out double sumTotal, params Order[] orders)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductImport));
            criteria.CreateAlias("Import", "import");

            if (criterion != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Ge("import.ImportDate", firstDate));
                criterion = Restrictions.And(criterion, Restrictions.Le("import.ImportDate", lastDate));
            }
            else
            {
                criterion = Restrictions.Ge("import.ImportDate", firstDate);
                criterion = Restrictions.And(criterion, Restrictions.Le("import.ImportDate", lastDate));
            }
            criteria.Add(criterion);
            foreach (Order order in orders)
            {
                if (order != null)
                {
                    criteria.AddOrder(order);
                }
            }

            if (pageSize > 0)
            {
                if (pageIndex <= 0) pageIndex = 0;
                criteria.SetFirstResult(pageSize * pageIndex);
                criteria.SetMaxResults(pageSize);
            }

            IList list = criteria.List();

            // Tính tổng Quantity và tổng số tiền
            criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductImport));
            criteria.CreateAlias("Import", "import");
            if (criterion != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Ge("import.ImportDate", firstDate));
                criterion = Restrictions.And(criterion, Restrictions.Le("import.ImportDate", lastDate));
            }
            else
            {
                criterion = Restrictions.Ge("import.ImportDate", firstDate);
                criterion = Restrictions.And(criterion, Restrictions.Le("import.ImportDate", lastDate));
            }
            criteria.Add(criterion);

            criteria.SetProjection(Projections.ProjectionList().Add(Projections.Sum("Quantity"), "quantity")
                .Add(Projections.Sum("Total"), "total"));
            IList listObj = criteria.List();

            object[] obj = (object[])listObj[0];

            if (obj[0] != null)
            {
                sumQuantity = (int)obj[0];
            }
            else
            {
                sumQuantity = 0;
            }
            if (obj[1] != null)
            {
                sumTotal = (double)obj[1];
            }
            else
            {
                sumTotal = 0;
            }

            return list;

        }
        /// <summary>
        /// đém số sản phẩm đã nhập
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="firstDate"></param>
        /// <param name="lastDate"></param>
        /// <returns></returns>
        public int ImportReportCountByCriterion(ICriterion criterion, DateTime firstDate, DateTime lastDate)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductImport));
            criteria.CreateAlias("Import", "import");
            if (criterion != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Ge("import.ImportDate", firstDate));
                criterion = Restrictions.And(criterion, Restrictions.Le("import.ImportDate", lastDate));
            }
            else
            {
                criterion = Restrictions.Ge("import.ImportDate", firstDate);
                criterion = Restrictions.And(criterion, Restrictions.Le("import.ImportDate", lastDate));
            }
            criteria.Add(criterion);

            criteria.SetProjection(Projections.RowCount());
            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (int)list[0];
            }
            else
            {
                return 0;
            }

        }



        #endregion

        #region ---ProductExport---
        /// <summary>
        /// lấy phiếu xuất
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="count"></param>
        /// <param name="sum"></param>
        /// <returns></returns>
        public IList IvExportGetByQueryString(NameValueCollection query, User user, int pageSize, int pageIndex, out int count,
                                                out double sum)
        {
            ICriterion criterion = Restrictions.Eq("Deleted", false);
            //criterion = Restrictions.And(criterion, Restrictions.Or(Restrictions.Eq("Status", IvExportType.Pay), Restrictions.Eq("Status", IvExportType.Paid)));
            criterion = Restrictions.And(criterion, Restrictions.Like("Name", query["name"], MatchMode.Anywhere));
            if (string.IsNullOrEmpty(query["name"]) && string.IsNullOrEmpty(query["code"]) &&
                string.IsNullOrEmpty(query["time"]))
            {
                //DateTime firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                //DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);

                //criterion = Restrictions.And(criterion, Restrictions.Ge("ExportDate", firstDay));
                //criterion = Restrictions.And(criterion, Restrictions.Le("ExportDate", lastDay));
            }

            if (!string.IsNullOrEmpty(query["name"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Name", query["name"], MatchMode.Anywhere));
            }
            if (!string.IsNullOrEmpty(query["code"]))
            {
                criterion = Restrictions.And(criterion, Restrictions.Like("Code", query["code"], MatchMode.Anywhere));
            }
            if (!string.IsNullOrEmpty(query["time"]))
            {
                double time = Convert.ToDouble(query["time"]);
                DateTime FtimeConvert = DateTime.FromOADate(time);
                DateTime LtimeConvert = FtimeConvert.AddMonths(1).AddDays(-1);

                criterion = Restrictions.And(criterion, Restrictions.Ge("ExportDate", FtimeConvert));
                criterion = Restrictions.And(criterion, Restrictions.Le("ExportDate", LtimeConvert));
            }
            if (!string.IsNullOrEmpty(query["fromDate"]))
            {
                double time = Convert.ToDouble(query["fromDate"]);
                DateTime ffromDate = DateTime.FromOADate(time);
                DateTime ltimeConvert = ffromDate.AddDays(-1);
                criterion = Restrictions.And(criterion, Restrictions.Gt("ExportDate", ltimeConvert));
            }
            if (!string.IsNullOrEmpty(query["toDate"]))
            {
                double time = Convert.ToDouble(query["toDate"]);
                DateTime ffromDate = DateTime.FromOADate(time);
                DateTime ltimeConvert = ffromDate.AddDays(1);
                criterion = Restrictions.And(criterion, Restrictions.Lt("ExportDate", ltimeConvert));
            }
            if (!string.IsNullOrEmpty(query["debt"]))
            {
                if (query["debt"] == "0") criterion = Restrictions.And(criterion, Restrictions.Not(Restrictions.Eq("IsDebt", true)));
                else if (query["debt"] == "1") criterion = Restrictions.And(criterion, Restrictions.Eq("IsDebt", true));
            }
            AddStorageCriterion(ref criterion, user, query);


            Order order = RepeaterOrder.GetOrderFromQueryString(query);
            if (order == null)
            {
                order = Order.Desc("ExportDate");
            }

            count = IvExportCountByCriterion(criterion);
            sum = IvExportTotalByCriterion(criterion);
            return IvExportGetByCriterionPaged(criterion, pageSize, pageIndex, order);
        }
        /// <summary>
        /// đếm số phiếu xuất
        /// </summary>
        /// <param name="criterion"></param>
        /// <returns></returns>
        public int IvProductExportCountByCriterion(ICriterion criterion)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductExport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.RowCount());

            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (int)list[0];
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// lấy phiếu xuất
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="orders"></param>
        /// <returns></returns>

        public IList IvExportGetByCriterionPaged(ICriterion criterion, int pageSize, int pageIndex, params Order[] orders)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvExport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }
            foreach (Order order in orders)
            {
                if (order != null)
                {
                    criteria.AddOrder(order);
                }
            }

            if (pageSize > 0)
            {
                if (pageIndex <= 0) pageIndex = 0;
                criteria.SetFirstResult(pageSize * pageIndex);
                criteria.SetMaxResults(pageSize);
            }
            return criteria.List();
        }
        /// <summary>
        /// đếm số lượng các sản phẩm đã xuất
        /// </summary>
        /// <param name="criterion"></param>
        /// <returns></returns>
        public double TotalProductExportByProductId(ICriterion criterion)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductExport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            criteria.SetProjection(Projections.Sum("QuanityRateParentUnit"));
            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (double)list[0];
            }
            else
            {
                return 0.0;
            }

        }
        /// <summary>
        /// lấy báo cáo các sản phẩm xuất
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="firstDate"></param>
        /// <param name="lastDate"></param>
        /// <param name="sumQuantity"></param>
        /// <param name="sumTotal"></param>
        /// <param name="orders"></param>
        /// <returns></returns>
        public IList ExportReportGetByCriterionPaged(ICriterion criterion, int pageSize, int pageIndex, DateTime firstDate, DateTime lastDate, out int sumQuantity, out double sumTotal, params Order[] orders)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductExport));
            criteria.CreateAlias("Export", "export");

            if (criterion != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Ge("export.ExportDate", firstDate));
                criterion = Restrictions.And(criterion, Restrictions.Le("export.ExportDate", lastDate));

            }
            else
            {
                criterion = Restrictions.Ge("export.ExportDate", firstDate);
                criterion = Restrictions.And(criterion, Restrictions.Le("export.ExportDate", lastDate));
            }
            criteria.Add(criterion);
            foreach (Order order in orders)
            {
                if (order != null)
                {
                    criteria.AddOrder(order);
                }
            }

            if (pageSize > 0)
            {
                if (pageIndex <= 0) pageIndex = 0;
                criteria.SetFirstResult(pageSize * pageIndex);
                criteria.SetMaxResults(pageSize);
            }

            IList list = criteria.List();

            // Tính tổng Quantity và tổng số tiền
            criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductExport));
            criteria.CreateAlias("Export", "export");

            if (criterion != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Ge("export.ExportDate", firstDate));
                criterion = Restrictions.And(criterion, Restrictions.Le("export.ExportDate", lastDate));

            }
            else
            {
                criterion = Restrictions.Ge("export.ExportDate", firstDate);
                criterion = Restrictions.And(criterion, Restrictions.Le("export.ExportDate", lastDate));
            }
            criteria.Add(criterion);
            criteria.SetProjection(Projections.ProjectionList().Add(Projections.Sum("Quantity"), "quantity")
                .Add(Projections.Sum("Total"), "total"));

            IList listObj = criteria.List();

            object[] obj = (object[])listObj[0];

            if (obj[0] != null)
            {
                sumQuantity = (int)obj[0];
            }
            else
            {
                sumQuantity = 0;
            }
            if (obj[1] != null)
            {
                sumTotal = (double)obj[1];
            }
            else
            {
                sumTotal = 0;
            }

            return list;

        }
        /// <summary>
        /// báo cáo đếm các sẩn phẩm xuất
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="firstDate"></param>
        /// <param name="lastDate"></param>
        /// <returns></returns>
        public int ExportReportCountByCriterion(ICriterion criterion, DateTime firstDate, DateTime lastDate)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductExport));
            criteria.CreateAlias("Export", "export");
            if (criterion != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Ge("export.ExportDate", firstDate));
                criterion = Restrictions.And(criterion, Restrictions.Le("export.ExportDate", lastDate));
            }
            else
            {
                criterion = Restrictions.Ge("export.ExportDate", firstDate);
                criterion = Restrictions.And(criterion, Restrictions.Le("export.ExportDate", lastDate));
            }

            criteria.Add(criterion);
            criteria.SetProjection(Projections.RowCount());

            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (int)list[0];
            }
            else
            {
                return 0;
            }

        }

        public object[] HistoryCountAndSumByCriterion(ICriterion criterion, IvProduct product)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductExport));
            criteria.CreateAlias("Export", "export");

            if (criterion != null)
            {
                criterion = Restrictions.And(criterion, Restrictions.Eq("Product", product));

            }
            else
            {
                criterion = Restrictions.Eq("Product", product);
            }
            criteria.Add(criterion);
            criteria.SetProjection(Projections.ProjectionList().Add(Projections.Sum("Quantity"))
                .Add(Projections.Sum("Total")));
            IList list = criteria.List();
            if (list.Count > 0 && list[0] != null)
            {
                return (object[])criteria.List()[0];
            }
            else
            {
                return null;
            }
        }

        public IList HistoryGetByCriterion(ICriterion criterion, int pageSize, int pageIndex, double firstDate, double lastDate, out int count, params Order[] orders)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductExport));
            criteria.CreateAlias("Export", "export", NHibernate.SqlCommand.JoinType.InnerJoin);
            criteria.CreateAlias("Product", "product", NHibernate.SqlCommand.JoinType.InnerJoin);
            // co criterion va co ngay thang
            // co criterion va ko co ngay thang
            // ko criterion va co co ngay thang
            // ko criterion va ko co ngay thang

            ICriterion dateCrit = null;
            if (firstDate > 0)
            {
                DateTime fromDate = DateTime.FromOADate(firstDate);
                dateCrit = Restrictions.Ge("export.ExportDate", fromDate);
            }
            if (lastDate > 0)
            {
                DateTime toDate = DateTime.FromOADate(lastDate);
                if (dateCrit != null)
                {
                    dateCrit = Restrictions.And(dateCrit, Restrictions.Le("export.ExportDate", toDate));
                }
                else
                {
                    dateCrit = Restrictions.Le("export.ExportDate", toDate);
                }
            }

            if (criterion != null && dateCrit != null)
            {
                criterion = Restrictions.And(criterion, dateCrit);
            }
            else if (dateCrit != null)
            {
                criterion = dateCrit;
            }

            if (criterion != null)
            {
                criteria.Add(criterion);
            }

            //criteria.SetProjection();
            criteria.SetProjection(Projections.ProjectionList().Add(Projections.Distinct(Projections.GroupProperty("Product"))) // Dau tien la product
                .Add(Projections.Sum("Quantity"), "quantity") // Tiep theo la quantity cua product do
                .Add(Projections.Sum("Total"), "total")); // Tiep theo la tong gia tri
            //Projections.CountDistinct

            //criteria.AddOrder(Order.Desc("quantity"));

            foreach (Order order in orders)
            {
                if (order != null)
                {
                    criteria.AddOrder(order);
                }
            }

            if (pageSize > 0)
            {
                if (pageIndex <= 0) pageIndex = 0;
                criteria.SetFirstResult(pageSize * pageIndex);
                criteria.SetMaxResults(pageSize);
            }

            IList list = criteria.List();

            IList result = new ArrayList();
            foreach (object[] productInfo in list)
            {
                IvProduct product = (IvProduct)productInfo[0];
                product.SumQuantity = (int)productInfo[1];
                product.SumTotal = (double)productInfo[2];
                result.Add(product);
            }

            criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductExport));
            criteria.CreateAlias("Export", "export");
            criteria.CreateAlias("Product", "product");

            criteria.SetProjection(Projections.CountDistinct("Product"));
            count = (int)criteria.List()[0];

            return result;
        }

        /// <summary>
        /// đếm phiếu xuất theo ngày
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public int CountExportByDateTime(NameValueCollection query, User user, DateTime date)
        {
            ICriterion criterion = Restrictions.And(Restrictions.Gt("ExportDate", date.AddDays(-1)), Restrictions.Lt("ExportDate", date.AddDays(1)));
            //AddStorageCriterion(ref criterion, user, query);
            return _commonDao.CountObjectByCriterion(typeof(IvExport), criterion);
        }
        /// <summary>
        /// đếm phiếu xuất theo mã
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public int CountExportGetByCode(string code)
        {
            ICriterion criterion = Restrictions.Eq("Code", code);
            return _commonDao.CountObjectByCriterion(typeof(IvExport), criterion);
        }
        /// <summary>
        /// lấy sản phẩm bán ra theo phiếu xuất
        /// </summary>
        /// <param name="export"></param>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IList IvProductExportGetByExport(IvExport export, NameValueCollection query, int pageSize,
            int pageIndex, out int count)
        {
            ICriterion criterion = Restrictions.Eq("Export", export);
            Order order = RepeaterOrder.GetOrderFromQueryString(query);
            if (order == null)
            {
                order = Order.Desc("Id");
            }

            count = IvProductExportCountByCriterion(criterion);
            return IvProductExportGetByCriterionPaged(criterion, pageSize, pageIndex, order);
        }
        /// <summary>
        /// lấy các sản phẩm bán ra theo phiếu xuất
        /// </summary>
        /// <param name="exportId"></param>
        /// <returns></returns>
        public IList<IvProductExport> IvProductExportGetByExport(int exportId)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<IvProductExport>();
            queryOver.Where(r => r.Export.Id == exportId);
            return queryOver.List<IvProductExport>();
        }
        /// <summary>
        ///  lấy các sản phẩm bán ra
        /// </summary>
        /// <param name="criterion"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="orders"></param>
        /// <returns></returns>
        public IList IvProductExportGetByCriterionPaged(ICriterion criterion, int pageSize, int pageIndex, params Order[] orders)
        {
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(IvProductExport));
            if (criterion != null)
            {
                criteria.Add(criterion);
            }
            foreach (Order order in orders)
            {
                if (order != null)
                {
                    criteria.AddOrder(order);
                }
            }

            if (pageSize > 0)
            {
                if (pageIndex <= 0) pageIndex = 0;
                criteria.SetFirstResult(pageSize * pageIndex);
                criteria.SetMaxResults(pageSize);
            }
            return criteria.List();
        }
        #endregion
        /// <summary>
        /// đếm số sản phẩm đã nhập theo 1 sp
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public int SumProductImport(NameValueCollection query, User user, IvProduct product)
        {
            ICriterion criterion = Restrictions.Eq("Product", product);
            AddStorageCriterion(ref criterion, user, query);
            return TotalProductImportByProductId(criterion);
        }
        /// <summary>
        /// đếm số sản phẩm đã nhập trong kho
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public int SumProductImport(IvStorage storage, IvProduct product)
        {
            ICriterion criterion = Restrictions.Eq("Product", product);
            criterion = Restrictions.And(criterion, Restrictions.Eq("Storage.Id", storage.Id));
            return TotalProductImportByProductId(criterion);
        }
        /// <summary>
        /// đếm số sản phẩm đã xuất
        /// </summary>
        /// <param name="query"></param>
        /// <param name="user"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public double SumProductExport(NameValueCollection query, User user, IvProduct product)
        {
            ICriterion criterion = Restrictions.Eq("Product", product);
            AddStorageCriterion(ref criterion, user, query);
            return TotalProductExportByProductId(criterion);
        }
        /// <summary>
        /// đếm số sản phẩm đã xuất
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public double SumProductExport(IvStorage storage, IvProduct product)
        {
            ICriterion criterion = Restrictions.Eq("Product", product);
            criterion = Restrictions.And(criterion, Restrictions.Eq("Storage.Id", storage.Id));
            return TotalProductExportByProductId(criterion);
        }
        /// <summary>
        /// lấy cấu hình cảnh báo sản phẩm sắp hết
        /// </summary>
        /// <param name="ivStorage"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public double GetWarningLimit(IvStorage ivStorage, IvProduct product)
        {
            var productWarning = GetProductWarning(product, ivStorage);
            if (productWarning != null)
                return productWarning.WarningLimit;
            return 0;
        }
        #endregion

        #region -- auto tool --

        public IList<BookingRoom> BookingRoomGetRoomNull()
        {
            //var query = _commonDao.OpenSession().QueryOver<BookingRoom>();
            //query = query.WhereRestrictionOn(x => x.Room).IsNull;
            //Booking booking = null;
            //query = query.JoinAlias(x => x.Book, () => booking);
            //query = query.Where(x => booking.StartDate >= DateTime.Today);
            //return query.List();
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(BookingRoom));
            criteria.CreateAlias("Book", "booking");
            criteria.Add(Restrictions.IsNull("Room"));
            criteria.Add(Restrictions.Eq("booking.Deleted", false));
            criteria.Add(Restrictions.Ge("booking.StartDate", DateTime.Today));
            criteria.Add(Restrictions.Not(Restrictions.Eq("booking.Status", StatusType.Cancelled)));
            return criteria.List<BookingRoom>();
        }

        public IList<Booking> BookingGetAllFromToDay()
        {
            //            var query = _commonDao.OpenSession().QueryOver<Booking>();
            //            query = query.Where(x => x.Deleted == false);
            //            query = query.Where(x => x.StartDate >= DateTime.Today);
            //            query = query.Where(x => x.Status != StatusType.Cancelled);
            //            return query.OrderBy(b => b.StartDate).Asc.List();
            ICriteria criteria = _commonDao.OpenSession().CreateCriteria(typeof(Booking));
            criteria.Add(Restrictions.Eq("Deleted", false));
            criteria.Add(Restrictions.Ge("StartDate", DateTime.Today));
            criteria.Add(Restrictions.Not(Restrictions.Eq("Status", StatusType.Cancelled)));
            return criteria.List<Booking>();
        }
        public IList<vBookingRoomNull> GetBookingRoomNull()
        {
            var query = _commonDao.OpenSession().QueryOver<vBookingRoomNull>();
            query = query.Where(x => x.Status != StatusType.Cancelled);
            return query.Take(300).List();
        }
        public List<Room> RoomGetAll()
        {
            //var query = _commonDao.OpenSession().QueryOver<Room>();
            //query = query.Where(x => x.Deleted == false);
            //return query.List();
            var list = _sailsDao.RoomGetAll(false);
            var rooms = new List<Room>();
            foreach (Room room in list)
            {
                rooms.Add(room);
            }
            return rooms;
        }

        #endregion
        /// <summary>
        /// lấy lịch sử thanh toán  booking
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IList<Transaction> GetTransactionHistory(NameValueCollection query)
        {
            var queryOver = _commonDao.OpenSession().QueryOver<Transaction>();
            DateTime from = DateTimeUtil.DateGetDefaultFromDate();

            if (query["f"] != null)
                from = DateTime.ParseExact(query["f"], "dd/MM/yyyy", CultureInfo.InvariantCulture);
            from = from.AddDays(-1);

            DateTime to = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

            if (query["t"] != null)
                to = DateTime.ParseExact(query["t"], "dd/MM/yyyy", CultureInfo.InvariantCulture);

            to = to.AddDays(1);
            queryOver = queryOver.Where(x => x.CreatedDate > from);


            queryOver = queryOver.Where(x => x.CreatedDate < to);


            if (query["gc"] != null)
                queryOver = queryOver.Where(x => x.TransactionGroup.Id == Convert.ToInt32(query["gc"]));
            return queryOver.List();
        }

    }

    #region -- Nested --
    public class RoomStatus
    {
        public RoomStatus()
        {
            BookingRooms = new List<BookingRoom>();
        }

        public Room Room { get; set; }

        /// <summary>
        /// Trạng thái phòng: 0 là trống, -1 là full, 1 là occuppied nhưng trong booking hiện tại (editable)
        /// </summary>
        public int Status { get; set; }

        public IList<BookingRoom> BookingRooms { get; set; }
    }
    #endregion

    #region -- Workaround --

    internal class CustomerServiceRepeaterHandler
    {
        private readonly Customer _customer;
        private readonly SailsModule _module;

        public CustomerServiceRepeaterHandler(Customer customer, SailsModule module)
        {
            _customer = customer;
            _module = module;
        }

        public void ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.DataItem is ExtraOption)
            {
                ExtraOption service = (ExtraOption)e.Item.DataItem;
                CheckBox chkService = (CheckBox)e.Item.FindControl("chkService");
                HiddenField hiddenId = (HiddenField)e.Item.FindControl("hiddenId");
                //HiddenField hiddenService = (HiddenField) e.Item.FindControl("hiddenService");

                // Lấy dữ liệu về dịch vụ, ưu tiên theo customer, nếu không có thì sử dụng booking
                // Lấy theo customer
                CustomerService customerService = _module.CustomerServiceGetByCustomerAndService(_customer, service);
                if (customerService != null)
                {
                    chkService.Checked = !customerService.IsExcluded;
                    hiddenId.Value = customerService.Id.ToString();
                    return;
                }
            }
        }

        public static void Save(Repeater rptServices, SailsModule Module, Customer customer)
        {
            foreach (RepeaterItem serviceItem in rptServices.Items)
            {
                // Cần phải biết mặc định là có hay không sử dụng dịch vụ

                // Nếu mặc định khác với thực tế hoặc đã có dữ liệu trong CSDL thì lưu lại
                CheckBox chkService = (CheckBox)serviceItem.FindControl("chkService");
                HiddenField hiddenId = (HiddenField)serviceItem.FindControl("hiddenId");
                HiddenField hiddenServiceId = (HiddenField)serviceItem.FindControl("hiddenServiceId");

                if (string.IsNullOrEmpty(hiddenId.Value) && chkService.Checked)
                {
                    CustomerService service = new CustomerService();
                    service.Service = Module.ExtraOptionGetById(Convert.ToInt32(hiddenServiceId.Value));
                    service.Customer = customer;
                    service.IsExcluded = false;
                    Module.SaveOrUpdate(service);
                    continue;
                }

                if (hiddenId.Value == "0" && !chkService.Checked)
                {
                    CustomerService service = new CustomerService();
                    service.Service = Module.ExtraOptionGetById(Convert.ToInt32(hiddenServiceId.Value));
                    service.Customer = customer;
                    service.IsExcluded = true;
                    Module.SaveOrUpdate(service);
                    continue;
                }

                if (!string.IsNullOrEmpty(hiddenId.Value) && hiddenId.Value != "0")
                {
                    CustomerService service = Module.CustomerServiceGetById(Convert.ToInt32(hiddenId.Value));
                    if (chkService.Checked == service.IsExcluded)
                    {
                        service.IsExcluded = !chkService.Checked;
                        Module.SaveOrUpdate(service);
                    }
                }
            }
        }

        public static IList GetCustomerServiceData(Repeater rptServices, SailsModule Module)
        {
            IList result = new ArrayList();
            foreach (RepeaterItem serviceItem in rptServices.Items)
            {
                // Cần phải biết mặc định là có hay không sử dụng dịch vụ

                // Nếu mặc định khác với thực tế hoặc đã có dữ liệu trong CSDL thì lưu lại
                CheckBox chkService = (CheckBox)serviceItem.FindControl("chkService");
                HiddenField hiddenServiceId = (HiddenField)serviceItem.FindControl("hiddenServiceId");

                CustomerService service = new CustomerService();
                service.Service = Module.ExtraOptionGetById(Convert.ToInt32(hiddenServiceId.Value));
                service.IsExcluded = !chkService.Checked;
                result.Add(service);
            }
            return result;
        }
    }

    #endregion
}