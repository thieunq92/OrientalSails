﻿using NHibernate;
using Portal.Modules.OrientalSails.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal.Modules.OrientalSails.Repository
{
    public class ServiceOutsideRepository : RepositoryBase<ServiceOutside>
    {
        public ServiceOutsideRepository() : base() { }
        public ServiceOutsideRepository(ISession session) : base(session) { }

        public IList<ServiceOutside> ServiceOutsideGetAllByBookingId(int bookingId)
        {
            return _session.QueryOver<ServiceOutside>().Where(x => x.Booking.Id == bookingId).List();
        }
    }
}