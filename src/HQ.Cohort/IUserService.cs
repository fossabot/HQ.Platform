﻿// Copyright (c) HQ.IO Corporation. All rights reserved.
// Licensed under the Reciprocal Public License, Version 1.5. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HQ.Cohort.Models;
using HQ.Rosetta;
using Microsoft.AspNetCore.Identity;

namespace HQ.Cohort
{
	public interface IUserService<TUser> where TUser : IdentityUser
	{
		IQueryable<TUser> Users { get; }
		Task<Operation<TUser>> CreateAsync(CreateUserModel model);
		Task<Operation<TUser>> FindByIdAsync(string id);
		Task<Operation<TUser>> FindByEmailAsync(string email);
		Task<Operation<TUser>> FindByNameAsync(string username);
		Task<Operation<TUser>> FindByPhoneNumberAsync(string phoneNumber);
		Task<Operation<TUser>> FindByLoginAsync(string loginProvider, string providerKey);
		Task<Operation<TUser>> FindByAsync(Expression<Func<TUser, bool>> predicate);

		Task<Operation<IList<string>>> GetRolesAsync(TUser user);
		Task<Operation> AddToRoleAsync(TUser user, string role);
		Task<Operation> RemoveFromRoleAsync(TUser user, string role);

		Task<Operation<string>> GenerateChangePhoneNumberTokenAsync(TUser user, string phoneNumber);
		Task<Operation<string>> GenerateChangeEmailTokenAsync(TUser user, string newEmail);
		Task<Operation<string>> GenerateEmailConfirmationTokenAsync(TUser user);
		Task<Operation<string>> GeneratePasswordResetTokenAsync(TUser user);
		Task<Operation<IEnumerable<string>>> GenerateNewTwoFactorRecoveryCodesAsync(TUser user, int number);

		Task<Operation> ChangePhoneNumberAsync(TUser user, string phoneNumber, string token);
		Task<Operation> ChangeEmailAsync(TUser user, string newEmail, string token);
		Task<Operation> ChangePasswordAsync(TUser user, string token, string newPassword);
		Task<Operation> ConfirmEmailAsync(TUser user, string token);
		Task<Operation> ResetPasswordAsync(TUser user, string token, string newPassword);


		Task<Operation> UpdateAsync(TUser user);
	}
}