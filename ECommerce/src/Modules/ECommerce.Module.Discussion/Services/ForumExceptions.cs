using System;

namespace ECommerce.Module.Discussion.Services
{
    public class ForumNotFoundException : Exception
    {
        public ForumNotFoundException(string message = "Not found") : base(message)
        {
        }
    }

    public class ForumConflictException : Exception
    {
        public ForumConflictException(string message) : base(message)
        {
        }
    }
}
