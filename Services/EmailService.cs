namespace ecommerce.Services
{
    public class EmailService
    {
        public static void SendResetPasswordEmail(string email, string token)
        {
            // Replace with actual email sending logic
            var resetLink = $"https://yourdomain.com/reset-password?token={token}";
            Console.WriteLine($"Password reset link: {resetLink}");
            // Use your email sending service to send the email here
        }
    }

}
