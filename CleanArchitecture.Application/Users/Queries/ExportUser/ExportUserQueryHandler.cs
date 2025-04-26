using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.CsvFiles;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.ExportUser
{
    internal sealed class ExportUserQueryHandler(IApplicationUserManager applicationUserManager,
        ICsvFileBuilder fileBuilder) : IQueryHandler<ExportUserQuery, ExportViewModel>
    {
        private readonly IApplicationUserManager _applicationUserManager = applicationUserManager;
        private readonly ICsvFileBuilder _fileBuilder = fileBuilder;

        public async Task<OperationResult<ExportViewModel>> Handle(ExportUserQuery request, CancellationToken cancellationToken)
        {
           
          var Users=  await _applicationUserManager
                                        .GetUsers(new GetUser.GetUserQuery(null), cancellationToken);

            var csv = new ExportViewModel(_fileBuilder.BuildUsersFile(Users),nameof(Users));
            return csv;
        }
    }
}
