using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class TransferUserFilesRequest
    {
        [Required]
        public int TargetUserId { get; set; }

        public TransferUserDependenciesRequest? Dependencies { get; set; }
    }

    public class TransferUserDependenciesRequest
    {
        public bool LoanOwnership { get; set; }

        public bool LoanOfficerAssignments { get; set; }

        public bool ExportPackageHistory { get; set; }

        public bool CreditReportHistory { get; set; }
    }
}
