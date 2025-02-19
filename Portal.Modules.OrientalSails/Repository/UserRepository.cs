﻿using CMS.Core.Domain;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal.Modules.OrientalSails.Repository
{
    public class UserRepository : RepositoryBase<User>
    {
        public UserRepository() : base() { }

        public UserRepository(ISession session) : base(session) { }


        public User UserGetById(int userId)
        {
            return _session.QueryOver<User>()
                .Where(x => x.IsActive == true)
                .Where(x => x.Id == userId).Take(1).SingleOrDefault();
        }

        public string UserGetName(int userId)
        {
            return UserGetById(userId).FullName;
        }

        public object UserGetByRole(int roleId)
        {
            Role roleAlias = null;
            return _session.QueryOver<User>()
                .Where(x => x.IsActive == true)
                .JoinAlias(x => x.Roles, () => roleAlias)
                .Where(x => roleAlias.Id == roleId).List();
        }

        public IEnumerable<User> SalesGetAll()
        {
            Role roleAlias = null;
            return _session.QueryOver<User>()
                .Where(x => x.IsActive == true)
                .JoinAlias(x => x.Roles, () => roleAlias)
                .Where(x => roleAlias.Name == "Sales").List();
        }
    }
}