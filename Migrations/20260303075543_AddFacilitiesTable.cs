using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Salamaty.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilitiesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.CreateTable(
                name: "Facilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperatingHours = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Governorate = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facilities", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Facilities");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetSpecialty = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "CreatedAt", "IsRead", "Message", "TargetSpecialty", "Title", "Type", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(5753), false, "Your journey to better healthcare starts here. Find the nearest specialized hospital now.", "General", "Welcome to Salamaty! ", "Welcome", "All" },
                    { 2, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(5856), false, "Drinking enough water regularly is your first line of defense against kidney stones.", "Nephrology", "Kidney Safety ", "Awareness", "All" },
                    { 3, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(5944), false, "Persistent back pain may require consulting an orthopedic specialist. Don't ignore your body's signals.", "Orthopedics", "Bone Health ", "Awareness", "All" },
                    { 4, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(6151), false, "International Eye Hospital in Cairo is now available for fundus examinations.", "Ophthalmology", "Eye Specialist Nearby ", "Proximity", "All" },
                    { 5, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(6233), false, "5 new centers specializing in maternal and child care have been added in Giza.", "Obstetrics & Gynecology", "OB-GYN Section ", "Update", "All" },
                    { 6, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(6309), false, "Brisk walking for 20 minutes improves heart muscle efficiency amazingly.", "Cardiology", "Heart Health ", "Awareness", "All" },
                    { 7, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(6392), false, "Make sure to review your child's periodic vaccination schedule at the nearest pediatric center.", "Pediatrics", "Child Vaccinations ", "Awareness", "All" },
                    { 8, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(6467), false, "In cases of sudden, severe headaches, please head to the nearest neurology emergency immediately.", "Neurology", "Important Alert ", "Emergency", "All" },
                    { 9, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(6543), false, "Visiting a dentist every 6 months prevents worsening of decay and gum problems.", "Dentistry", "Healthy Smile ", "Awareness", "All" },
                    { 10, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(6621), false, "You can now find accredited dialysis centers in Fayoum and Minya governorates.", "Nephrology", "New Kidney Centers ", "Update", "All" },
                    { 11, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(6710), false, "Regular follow-ups with a gynecologist ensure a healthy and safe pregnancy journey for you and your baby.", "Obstetrics & Gynecology", "Safe Pregnancy ", "Awareness", "All" },
                    { 12, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(6873), false, "Al Salam International Hospital now offers specialized consultations in joint and spinal surgery.", "Orthopedics", "Orthopedic Clinic Available ", "Proximity", "All" },
                    { 13, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(6952), false, "Reducing salt in food protects arteries and reduces the risk of high blood pressure.", "Cardiology", "Nutrition & Heart ", "Awareness", "All" },
                    { 14, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7033), false, "Limit children's screen time to protect their eyes from early nearsightedness.", "Ophthalmology", "Protect Your Child's Vision ", "Awareness", "All" },
                    { 15, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7121), false, "Online booking is now active for \"Misr Center for Dentistry\" via the app.", "Dentistry", "Dental Services ", "Update", "All" },
                    { 16, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7197), false, "Maintaining sugar and pressure levels protects the brain from stroke risks.", "Neurology", "Stroke Prevention ", "Awareness", "All" },
                    { 17, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7275), false, "Tabarak Children's Hospital is equipped to receive critical emergency cases 24/7.", "Pediatrics", "Pediatric Emergency ", "Proximity", "All" },
                    { 18, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7354), false, "Calcium and Vitamin D rich foods are essential for building strong bones at all ages.", "Orthopedics", "Osteoporosis ", "Awareness", "All" },
                    { 19, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7508), false, "For allergy and respiratory patients, please stay home and use preventive medications.", "General", "Dust Storm Alert ", "Emergency", "All" },
                    { 20, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7584), false, "Periodic kidney function tests are vital, especially for diabetes and blood pressure patients.", "Nephrology", "Kidney Screenings ", "Awareness", "All" },
                    { 21, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7655), false, "Correct posture at the computer avoids chronic neck and back pain.", "Orthopedics", "Back Exercises ", "Awareness", "All" },
                    { 22, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7735), false, "15% discount on vision correction surgeries at selected eye centers for Salamaty users.", "Ophthalmology", "Eye Care Discounts ", "Update", "All" },
                    { 23, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7808), false, "If you feel an irregular heartbeat, consult a cardiologist as soon as possible.", "Cardiology", "Heartbeats ", "Awareness", "All" },
                    { 24, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7898), false, "Cairo Specialized Hospital includes top doctors in all medical specialties.", "Multidisciplinary", "Specialty Complex Nearby ", "Proximity", "All" },
                    { 25, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(7984), false, "Breastfeeding enhances your baby's immunity and protects them from infections in early months.", "Pediatrics", "Newborn Health ", "Awareness", "All" },
                    { 26, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(8068), false, "Nile Neurology Center has joined the Salamaty provider network.", "Neurology", "Neurology Clinics ", "Update", "All" },
                    { 27, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(8181), false, "Testing blood sugar in the 24th week of pregnancy is very important for mother and baby.", "Obstetrics & Gynecology", "Gestational Diabetes ", "Awareness", "All" },
                    { 28, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(8279), false, "Drinking plenty of water and flossing prevents bacteria that causes bad breath.", "Dentistry", "Oral Hygiene ", "Awareness", "All" },
                    { 29, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(8386), false, "Cairo Center for Kidney Diseases provides the latest care technologies for kidney patients.", "Nephrology", "Kidney Unit Nearby ", "Proximity", "All" },
                    { 30, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(8600), false, "Walking in mild sunlight helps the body produce Vitamin D essential for bones.", "Orthopedics", "Walking & Bone Health ", "Awareness", "All" },
                    { 31, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(8705), false, "You can now press the \"Emergency\" button to reach the nearest general hospital in your area.", "General", "Emergency Services ", "Update", "All" },
                    { 32, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(8954), false, "Avoiding fried foods preserves heart valves and body arteries.", "Cardiology", "Triglycerides ", "Awareness", "All" },
                    { 33, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(9064), false, "Using moisturizing drops is necessary if you spend long hours in air-conditioned environments.", "Ophthalmology", "Dry Eye ", "Awareness", "All" },
                    { 34, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(9169), false, "Al Meswak Dental Clinics are now available in your area at competitive prices.", "Dentistry", "Excellent Dentist ", "Proximity", "All" },
                    { 35, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(9270), false, "Monitor your child's height and weight monthly to ensure healthy and normal growth.", "Pediatrics", "Child Growth ", "Awareness", "All" },
                    { 36, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(9366), false, "We improved the search engine to help you reach the required specialty faster.", "General", "Salamaty App ", "Update", "All" },
                    { 37, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(9572), false, "Avoid noise and strong lighting when feeling the onset of a migraine attack.", "Neurology", "Migraine ", "Awareness", "All" },
                    { 38, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(9689), false, "Caring for mental and physical health after birth is as important as during pregnancy.", "Obstetrics & Gynecology", "Postpartum Care ", "Awareness", "All" },
                    { 39, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(9798), false, "You can now book an appointment at Cleopatra Orthopedic Center directly from the app.", "Orthopedics", "Orthopedic Specialist ", "Proximity", "All" },
                    { 40, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(9900), false, "Moderate protein intake reduces the burden on kidneys and maintains their function.", "Nephrology", "Kidney Protein ", "Awareness", "All" },
                    { 41, new DateTime(2026, 3, 2, 15, 29, 2, 24, DateTimeKind.Local).AddTicks(9988), false, "Addition of new labs specializing in genetic and hereditary tests.", "General", "Lab Centers ", "Update", "All" },
                    { 42, new DateTime(2026, 3, 2, 15, 29, 2, 25, DateTimeKind.Local).AddTicks(85), false, "Eating Omega-3 found in fish raises good cholesterol levels in your blood.", "Cardiology", "Good Cholesterol ", "Awareness", "All" },
                    { 43, new DateTime(2026, 3, 2, 15, 29, 2, 25, DateTimeKind.Local).AddTicks(178), false, "Early vision screening for preschoolers prevents \"lazy eye\" problems.", "Ophthalmology", "Pediatric Eye Exam ", "Awareness", "All" },
                    { 44, new DateTime(2026, 3, 2, 15, 29, 2, 25, DateTimeKind.Local).AddTicks(288), false, "Al Salam International Hospital offers all medical specialties around the clock.", "Multidisciplinary", "Multidisciplinary Hospital ", "Proximity", "All" },
                    { 45, new DateTime(2026, 3, 2, 15, 29, 2, 25, DateTimeKind.Local).AddTicks(458), false, "Bleeding gums may signal vitamin deficiency or early inflammation; consult your doctor.", "Dentistry", "Gum Health ", "Awareness", "All" },
                    { 46, new DateTime(2026, 3, 2, 15, 29, 2, 25, DateTimeKind.Local).AddTicks(562), false, "Opening of a new Dar Al Fouad Hospital branch featuring precise and rare specialties.", "Multidisciplinary", "New Clinics ", "Update", "All" },
                    { 47, new DateTime(2026, 3, 2, 15, 29, 2, 25, DateTimeKind.Local).AddTicks(658), false, "Eating nuts and antioxidant-rich foods improves brain function and memory.", "Neurology", "Memory & Focus ", "Awareness", "All" },
                    { 48, new DateTime(2026, 3, 2, 15, 29, 2, 25, DateTimeKind.Local).AddTicks(765), false, "Using cotton clothes protects your child's sensitive skin from allergies and irritation.", "Pediatrics", "Cotton Clothes for Kids ", "Awareness", "All" },
                    { 49, new DateTime(2026, 3, 2, 15, 29, 2, 25, DateTimeKind.Local).AddTicks(844), false, "If you feel tremors and cold sweat, eat candy immediately and consult an internist.", "General", "Low Blood Sugar ", "Emergency", "All" },
                    { 50, new DateTime(2026, 3, 2, 15, 29, 2, 25, DateTimeKind.Local).AddTicks(954), false, "Adding your blood type and medical history helps us provide more accurate health recommendations.", "General", "Complete Your Profile ", "Welcome", "All" }
                });
        }
    }
}
