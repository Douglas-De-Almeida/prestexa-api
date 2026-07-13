namespace PrestexaAPI.Models
{
    public class FormSetItem
    {
        public int Id { get; set; }
        public int FormSetId { get; set; }
        public int FormDefinitionId { get; set; }
        public int DisplayOrder { get; set; }
    }
}