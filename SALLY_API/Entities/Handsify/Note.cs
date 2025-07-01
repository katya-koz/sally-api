namespace SALLY_API.Entities.Handsify
{
    public class Note
    {
        public string Content { get;  set; }
        public DateTime CreateDate { get;  set; }
        public string Author { get;  set; }

        public int NoteKey { get;  set; }


        public Note(string content, DateTime createDate, string author, int noteKey)
        {
            Content = content;
            CreateDate = createDate;
            Author = author;
            NoteKey = noteKey;
        }

    }
}
