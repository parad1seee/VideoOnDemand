namespace VideoOnDemand.Models
{
    public static class ModelRegularExpression
    {
        public const string REG_NOT_CONTAIN_SPACES_ONLY = "(^(?!\\s+$).+)"; 
        public const string REG_MUST_NOT_CONTAIN_SPACES = @"(^\S*$)";
        public const string REG_CANT_START_FROM_SPACES = "^\\S.*$";

        public const string REG_ONE_LATER_DIGIT = "^(?=.*[0-9])(?=.*[a-zA-Z])([a-zA-Z0-9_-]+)";
        public const string REG_ONE_LATER_DIGIT_WITH_SPEC = "^(?=.*[0-9])(?=.*[a-zA-Z])([a-zA-Z0-9\\s\\S_-]+)";

        public const string REG_EMAIL = "([a-zA-Z0-9]+([+=#-._][a-zA-Z0-9]+)*@([a-zA-Z0-9]+(-[a-zA-Z0-9]+)*)+(([.][a-zA-Z0-9]{2,4})*)?)";
        public const string REG_EMAIL_DOMAINS = "(^.{1,64}@.{1,64}$)";

        public const string REG_PHONE = @"^\+[0-9]+$";
        public const string REG_SMS_CODE = "^[0-9]+$";
    }
}
