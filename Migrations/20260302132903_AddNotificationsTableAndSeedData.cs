using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Salamaty.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsTableAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // إنشاء جدول الإشعارات
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetSpecialty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            // حقن البيانات الـ 50 إشعاراً بدون أيقونات
            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "CreatedAt", "IsRead", "Message", "TargetSpecialty", "Title", "Type", "UserId" },
                values: new object[,]
                {
                    { 1, DateTime.Now, false, "Your journey to better healthcare starts here. Find the nearest specialized hospital now.", "General", "Welcome to Salamaty", "Welcome", "All" },
                    { 2, DateTime.Now, false, "Drinking enough water regularly is your first line of defense against kidney stones.", "Nephrology", "Kidney Safety", "Awareness", "All" },
                    { 3, DateTime.Now, false, "Persistent back pain may require consulting an orthopedic specialist. Don't ignore your body's signals.", "Orthopedics", "Bone Health", "Awareness", "All" },
                    { 4, DateTime.Now, false, "International Eye Hospital in Cairo is now available for fundus examinations.", "Ophthalmology", "Eye Specialist Nearby", "Proximity", "All" },
                    { 5, DateTime.Now, false, "5 new centers specializing in maternal and child care have been added in Giza.", "Obstetrics & Gynecology", "OB-GYN Section", "Update", "All" },
                    { 6, DateTime.Now, false, "Brisk walking for 20 minutes improves heart muscle efficiency amazingly.", "Cardiology", "Heart Health", "Awareness", "All" },
                    { 7, DateTime.Now, false, "Make sure to review your child's periodic vaccination schedule at the nearest pediatric center.", "Pediatrics", "Child Vaccinations", "Awareness", "All" },
                    { 8, DateTime.Now, false, "In cases of sudden, severe headaches, please head to the nearest neurology emergency immediately.", "Neurology", "Important Alert", "Emergency", "All" },
                    { 9, DateTime.Now, false, "Visiting a dentist every 6 months prevents worsening of decay and gum problems.", "Dentistry", "Healthy Smile", "Awareness", "All" },
                    { 10, DateTime.Now, false, "You can now find accredited dialysis centers in Fayoum and Minya governorates.", "Nephrology", "New Kidney Centers", "Update", "All" },
                    { 11, DateTime.Now, false, "Regular follow-ups with a gynecologist ensure a healthy and safe pregnancy journey for you and your baby.", "Obstetrics & Gynecology", "Safe Pregnancy", "Awareness", "All" },
                    { 12, DateTime.Now, false, "Al Salam International Hospital now offers specialized consultations in joint and spinal surgery.", "Orthopedics", "Orthopedic Clinic Available", "Proximity", "All" },
                    { 13, DateTime.Now, false, "Reducing salt in food protects arteries and reduces the risk of high blood pressure.", "Cardiology", "Nutrition and Heart", "Awareness", "All" },
                    { 14, DateTime.Now, false, "Limit children's screen time to protect their eyes from early nearsightedness.", "Ophthalmology", "Protect Your Child's Vision", "Awareness", "All" },
                    { 15, DateTime.Now, false, "Online booking is now active for Misr Center for Dentistry via the app.", "Dentistry", "Dental Services", "Update", "All" },
                    { 16, DateTime.Now, false, "Maintaining sugar and pressure levels protects the brain from stroke risks.", "Neurology", "Stroke Prevention", "Awareness", "All" },
                    { 17, DateTime.Now, false, "Tabarak Children's Hospital is equipped to receive critical emergency cases 24/7.", "Pediatrics", "Pediatric Emergency", "Proximity", "All" },
                    { 18, DateTime.Now, false, "Calcium and Vitamin D rich foods are essential for building strong bones at all ages.", "Orthopedics", "Osteoporosis", "Awareness", "All" },
                    { 19, DateTime.Now, false, "For allergy and respiratory patients, please stay home and use preventive medications.", "General", "Dust Storm Alert", "Emergency", "All" },
                    { 20, DateTime.Now, false, "Periodic kidney function tests are vital, especially for diabetes and blood pressure patients.", "Nephrology", "Kidney Screenings", "Awareness", "All" },
                    { 21, DateTime.Now, false, "Correct posture at the computer avoids chronic neck and back pain.", "Orthopedics", "Back Exercises", "Awareness", "All" },
                    { 22, DateTime.Now, false, "15% discount on vision correction surgeries at selected eye centers for Salamaty users.", "Ophthalmology", "Eye Care Discounts", "Update", "All" },
                    { 23, DateTime.Now, false, "If you feel an irregular heartbeat, consult a cardiologist as soon as possible.", "Cardiology", "Heartbeats", "Awareness", "All" },
                    { 24, DateTime.Now, false, "Cairo Specialized Hospital includes top doctors in all medical specialties.", "Multidisciplinary", "Specialty Complex Nearby", "Proximity", "All" },
                    { 25, DateTime.Now, false, "Breastfeeding enhances your baby's immunity and protects them from infections in early months.", "Pediatrics", "Newborn Health", "Awareness", "All" },
                    { 26, DateTime.Now, false, "Nile Neurology Center has joined the Salamaty provider network.", "Neurology", "Neurology Clinics", "Update", "All" },
                    { 27, DateTime.Now, false, "Testing blood sugar in the 24th week of pregnancy is very important for mother and baby.", "Obstetrics & Gynecology", "Gestational Diabetes", "Awareness", "All" },
                    { 28, DateTime.Now, false, "Drinking plenty of water and flossing prevents bacteria that causes bad breath.", "Dentistry", "Oral Hygiene", "Awareness", "All" },
                    { 29, DateTime.Now, false, "Cairo Center for Kidney Diseases provides the latest care technologies for kidney patients.", "Nephrology", "Kidney Unit Nearby", "Proximity", "All" },
                    { 30, DateTime.Now, false, "Walking in mild sunlight helps the body produce Vitamin D essential for bones.", "Orthopedics", "Walking and Bone Health", "Awareness", "All" },
                    { 31, DateTime.Now, false, "You can now press the Emergency button to reach the nearest general hospital in your area.", "General", "Emergency Services", "Update", "All" },
                    { 32, DateTime.Now, false, "Avoiding fried foods preserves heart valves and body arteries.", "Cardiology", "Triglycerides", "Awareness", "All" },
                    { 33, DateTime.Now, false, "Using moisturizing drops is necessary if you spend long hours in air-conditioned environments.", "Ophthalmology", "Dry Eye", "Awareness", "All" },
                    { 34, DateTime.Now, false, "Al Meswak Dental Clinics are now available in your area at competitive prices.", "Dentistry", "Excellent Dentist", "Proximity", "All" },
                    { 35, DateTime.Now, false, "Monitor your child's height and weight monthly to ensure healthy and normal growth.", "Pediatrics", "Child Growth", "Awareness", "All" },
                    { 36, DateTime.Now, false, "We improved the search engine to help you reach the required specialty faster.", "General", "Salamaty App", "Update", "All" },
                    { 37, DateTime.Now, false, "Avoid noise and strong lighting when feeling the onset of a migraine attack.", "Neurology", "Migraine", "Awareness", "All" },
                    { 38, DateTime.Now, false, "Caring for mental and physical health after birth is as important as during pregnancy.", "Obstetrics & Gynecology", "Postpartum Care", "Awareness", "All" },
                    { 39, DateTime.Now, false, "You can now book an appointment at Cleopatra Orthopedic Center directly from the app.", "Orthopedics", "Orthopedic Specialist", "Proximity", "All" },
                    { 40, DateTime.Now, false, "Moderate protein intake reduces the burden on kidneys and maintains their function.", "Nephrology", "Kidney Protein", "Awareness", "All" },
                    { 41, DateTime.Now, false, "Addition of new labs specializing in genetic and hereditary tests.", "General", "Lab Centers", "Update", "All" },
                    { 42, DateTime.Now, false, "Eating Omega-3 found in fish raises good cholesterol levels in your blood.", "Cardiology", "Good Cholesterol", "Awareness", "All" },
                    { 43, DateTime.Now, false, "Early vision screening for preschoolers prevents lazy eye problems.", "Ophthalmology", "Pediatric Eye Exam", "Awareness", "All" },
                    { 44, DateTime.Now, false, "Al Salam International Hospital offers all medical specialties around the clock.", "Multidisciplinary", "Multidisciplinary Hospital", "Proximity", "All" },
                    { 45, DateTime.Now, false, "Bleeding gums may signal vitamin deficiency or early inflammation; consult your doctor.", "Dentistry", "Gum Health", "Awareness", "All" },
                    { 46, DateTime.Now, false, "Opening of a new Dar Al Fouad Hospital branch featuring precise and rare specialties.", "Multidisciplinary", "New Clinics", "Update", "All" },
                    { 47, DateTime.Now, false, "Eating nuts and antioxidant-rich foods improves brain function and memory.", "Neurology", "Memory and Focus", "Awareness", "All" },
                    { 48, DateTime.Now, false, "Using cotton clothes protects your child's sensitive skin from allergies and irritation.", "Pediatrics", "Cotton Clothes for Kids", "Awareness", "All" },
                    { 49, DateTime.Now, false, "If you feel tremors and cold sweat, eat candy immediately and consult an internist.", "General", "Low Blood Sugar", "Emergency", "All" },
                    { 50, DateTime.Now, false, "Adding your blood type and medical history helps us provide more accurate health recommendations.", "General", "Complete Your Profile", "Welcome", "All" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}