namespace CleanArchitecture.Application.Groups.Query.GroupMembers
{
    public class GroupMemberViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsCreator { get; set; }
    }

}
