﻿using dbsql_setup.Services;
using dbsql_setup.Interfaces;
using cafe_api_auth.Models;
using authentication_helper;
using cafe_api_auth.Interfaces;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using cafe_api_auth.Dtos;
using Mapster;

namespace cafe_api_auth.Services
{
    public class CustomerService : SqlBaseService<Customer>, ICustomerService
    {
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly ITokenHelper _tokenHelper;
        private readonly IUserHelper _userHelper;

        public CustomerService(
            ISqlGenericRepository<Customer> _genericRepository,
            IEncryptionHelper encryptionHelper,
            ITokenHelper tokenHelper,
            IUserHelper userHelper) : base(_genericRepository)
        {
            _encryptionHelper = encryptionHelper;
            _tokenHelper = tokenHelper;
            _userHelper = userHelper;
        }

        public AuthResponse<Customer> LoginUser(AuthRequest loginRequest, HttpRequest req)
        {
            var customer = Find(q => q.Email.Equals(loginRequest.Username)).FirstOrDefault();

            if (customer != null)
            {
                string hashedPassword = _encryptionHelper.CreatePasswordHash(loginRequest.Password, customer.PasswordSalt);
                bool isValid = hashedPassword.Equals(customer.Password);

                if (isValid)
                {
                    var accessToken = _tokenHelper.GenerateAccessToken(loginRequest.Username);
                    var refreshTokenInfo = _tokenHelper.GenerateRefreshToken(req);

                    return new AuthResponse<Customer>(isValid, accessToken, refreshTokenInfo, customer);
                }
            }

            return new AuthResponse<Customer>();
        }

        public CustomerDto GetUser(HttpRequest req)
        {
            var username = _userHelper.GetCurrentUserIdentity(req);

            if (string.IsNullOrEmpty(username))
                return null;

            var customer = Find(q => q.Email.Equals(username)).FirstOrDefault();

            if (customer != null)
                return customer.Adapt<CustomerDto>();

            return null;
        }
    }
}