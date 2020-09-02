namespace BugTracker.Web.Core.Persistence.Models
{
    public sealed class Project
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Active { get; set; }

        public int? DefaultUserId { get; set; }

        public int? AutoAssignDefaultUser { get; set; }

        public int? AutoSubscribeDefaultUser { get; set; }

        public int? EnablePop3 { get; set; }

        public string Pop3Username { get; set; }

        public string Pop3Password { get; set; }

        public string Pop3EmailFrom { get; set; }

        public int EnableCustomDropdown1 { get; set; }

        public int EnableCustomDropdown2 { get; set; }

        public int EnableCustomDropdown3 { get; set; }

        public string CustomDropdown1Label { get; set; }

        public string CustomDropdown2Label { get; set; }

        public string CustomDropdown3Label { get; set; }

        public string CustomDropdown1Values { get; set; }

        public string CustomDropdown2Values { get; set; }

        public string CustomDropdown3Values { get; set; }

        public int Default { get; set; }

        public string Description { get; set; }
    }
}