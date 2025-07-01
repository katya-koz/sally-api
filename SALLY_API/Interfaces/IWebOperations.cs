using SALLY_API.Entities;

namespace SALLY_API.Interfaces
{
    internal interface IWebOperations
    {

        public Task<UpsertStatus> Update(ADUser user);


        public Task<UpsertStatus> Create(ADUser user);

        public Task<HttpResponseMessage> Search(ADUser user);

        public Task Login();

    }
}
