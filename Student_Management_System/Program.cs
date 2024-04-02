using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StudentManagementSystem
{
    // Enums
    public enum Department
    {
        ComputerScience,
        BBA,
        English
    }

    public enum Degree
    {
        BSC,
        BBA,
        BA,
        MSC,
        MBA,
        MA
    }

    // Base class
    public class Person
    {
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
    }

    // Derived classes
    public class Student : Person
    {
        public string StudentID { get; set; }
        public Semester? JoiningBatch { get; set; }
        public Department Department { get; set; }
        public Degree Degree { get; set; }
        public List<Semester> SemestersAttended { get; set; }
        public List<Course> CoursesInSemester { get; set; }

        public Student()
        {
            SemestersAttended = new List<Semester>();
            CoursesInSemester = new List<Course>();
        }
    }

    public class Semester
    {
        public string? SemesterCode { get; set; }
        public string? Year { get; set; }

        public override string ToString()
        {
            return $"{SemesterCode} {Year}";
        }
    }

    public class Instructor : Person
    {
        public string InstructorID { get; set; }
        public Department Department { get; set; }
        public List<Course> CoursesTaught { get; set; }

        public Instructor()
        {
            CoursesTaught = new List<Course>();
        }
    }

    public class Course
    {
        public string? CourseID { get; set; }
        public string? CourseName { get; set; }
        public string? InstructorName { get; set; }
        public int NumberOfCredits { get; set; }
    }

    public static class CourseExtensions
    {
        public static Course GetCourseById(this List<Course> courses, string courseId)
        {
            return courses.FirstOrDefault(c => c.CourseID == courseId);
        }
    }

    // Attributes
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    sealed class DescriptionAttribute : Attribute
    {
        public string? Description { get; }

        // This is a positional argument
        public DescriptionAttribute(string description)
        {
            Description = description;
        }
    }

    // Interfaces
    internal interface IRepository<T>
    {
        void Add(T entity);
        T GetById(string id);
        void Delete(string id);
        void SaveChanges();
    }

    // Repository implementing interface
    internal class JsonRepository<T> : IRepository<T>
    {
        private List<T> entities;
        private string filePath;

        public List<T> GetAllEntities()
        {
            return entities;
        }


        // Event declarations
        public delegate void EntityAddedEventHandler(T entity);
        public event EntityAddedEventHandler EntityAdded;

        public delegate void EntityDeletedEventHandler(string id);
        public event EntityDeletedEventHandler EntityDeleted;

        public JsonRepository(string filePath)
        {
            this.filePath = filePath;
            LoadEntities();
        }

        public void Add(T entity)
        {
            entities.Add(entity);
            SaveChanges();

            EntityAdded?.Invoke(entity);
        }

        public T GetById(string id)
        {
            if (typeof(T) == typeof(Student))
            {
                var studentIdProperty = typeof(Student).GetProperty("StudentID");
                return entities.FirstOrDefault(e => studentIdProperty?.GetValue(e)?.ToString() == id);
            }
            else
            {
                var idProperty = typeof(T).GetProperty("ID");
                return entities.FirstOrDefault(e => idProperty?.GetValue(e)?.ToString() == id);
            }
        }


        public void Delete(string id)
        {
            try
            {
                if (typeof(T) == typeof(Student))
                {
                    var studentIdProperty = typeof(Student).GetProperty("StudentID");
                    var entityToRemove = entities.FirstOrDefault(e => studentIdProperty?.GetValue(e)?.ToString() == id);
                    if (entityToRemove != null)
                    {
                        entities.Remove(entityToRemove);
                        SaveChanges();
                        EntityDeleted?.Invoke(id);
                    }
                }
                else
                {
                    var idProperty = typeof(T).GetProperty("ID");
                    var entityToRemove = entities.FirstOrDefault(e => idProperty?.GetValue(e)?.ToString() == id);
                    if (entityToRemove != null)
                    {
                        entities.Remove(entityToRemove);
                        SaveChanges();
                        EntityDeleted?.Invoke(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting {typeof(T).Name.ToLower()}: {ex.Message}");
            }
        }



        public void SaveChanges()
        {
            string jsonOutput = JsonConvert.SerializeObject(entities);
            File.WriteAllText(filePath, jsonOutput);
        }

        private void LoadEntities()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                entities = JsonConvert.DeserializeObject<List<T>>(json);
            }
            else
            {
                entities = new List<T>();
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            string studentsFilePath = "students.json";
            IRepository<Student> studentRepository = new JsonRepository<Student>(studentsFilePath);

            string instructorsFilePath = "instructors.json";
            IRepository<Instructor> instructorRepository = new JsonRepository<Instructor>(instructorsFilePath);


            // Subscribe to events using lambda expressions
            (studentRepository as JsonRepository<Student>).EntityAdded += (student) =>
            {
                Console.WriteLine($"Student {student.FirstName} {student.LastName} added.");
            };

            // Subscribe to EntityDeleted event using anonymous method
            (studentRepository as JsonRepository<Student>).EntityDeleted += delegate (string id)
            {
                Console.WriteLine($"Entity with ID {id} deleted.");
            };

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("1. Add New Student");
                Console.WriteLine("2. View Student Details");
                Console.WriteLine("3. Delete Student");
                Console.WriteLine("4. Add New Semester and Courses");
                Console.WriteLine("5. Display Student List");
                Console.WriteLine("6. Exit");
                Console.Write("Select an option: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        AddNewEntity(studentRepository);
                        break;
                    case "2":
                        ViewEntityDetails(studentRepository);
                        break;
                    case "3":
                        DeleteEntity(studentRepository);
                        break;
                    case "4":
                        AddSemesterAndCourses(studentRepository);
                        break;
                    case "5":
                        DisplayStudentList(studentRepository);
                        break;
                    case "6":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        static void AddNewEntity<T>(IRepository<T> repository, params dynamic[] additionalInfo) where T : Person, new ()
        {
            try
            {
                T entity = new T();

                Console.Write("First Name: ");
                entity.FirstName = Console.ReadLine();

                Console.Write("Middle Name: ");
                entity.MiddleName = Console.ReadLine();

                Console.Write("Last Name: ");
                entity.LastName = Console.ReadLine();

                if (entity is Student student)
                {

                    Console.Write("Student ID: ");
                    student.StudentID = Console.ReadLine();

                    Console.Write("Joining Batch (Semester Code): ");
                    string semesterCode = Console.ReadLine();
                    Console.Write("Joining Year: ");
                    string year = Console.ReadLine();
                    student.JoiningBatch = new Semester { SemesterCode = semesterCode, Year = year };


                    Console.WriteLine("Select Department:");
                    foreach (var dept in Enum.GetValues(typeof(Department)))
                    {
                        Console.WriteLine($"{(int)dept}. {dept}");
                    }
                    Console.Write("Enter Department (by number): ");
                    if (Enum.TryParse(Console.ReadLine(), out Department deptChoice))
                    {
                        student.Department = deptChoice;
                    }
                    else
                    {
                        Console.WriteLine("Invalid department choice.");
                        return;
                    }

                    Console.WriteLine("Select Degree:");
                    foreach (var degree in Enum.GetValues(typeof(Degree)))
                    {
                        Console.WriteLine($"{(int)degree}. {degree}");
                    }
                    Console.Write("Enter Degree (by number): ");
                    if (Enum.TryParse(Console.ReadLine(), out Degree degreeChoice))
                    {
                        student.Degree = degreeChoice;
                    }
                    else
                    {
                        Console.WriteLine("Invalid degree choice.");
                        return;
                    }
                }

                    foreach (var info in additionalInfo)
                {
                    Console.Write(info.Description + ": ");
                    dynamic value = Console.ReadLine();
                    info.Value = value;
                }

                repository.Add(entity);
                Console.WriteLine($"{typeof(T).Name} Added Successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding {typeof(T).Name.ToLower()}: {ex.Message}");
            }
        }

        static void ViewEntityDetails<T>(IRepository<T> repository) where T : Person
        {
            try
            {
                Console.Write($"Enter {typeof(T).Name} ID: ");
                string id = Console.ReadLine();
                T entity = repository.GetById(id);
                if (entity != null)
                {
                    Console.WriteLine($"{typeof(T).Name} Details:");
                    Console.WriteLine($"Name: {entity.FirstName} {entity.MiddleName} {entity.LastName}");

                    if (typeof(T) == typeof(Student))
                    {
                        var student = entity as Student;
                        Console.WriteLine($"Student ID: {student.StudentID}");
                        Console.WriteLine($"Joining Batch: {student.JoiningBatch?.ToString()}");
                        Console.WriteLine($"Department: {student.Department}");
                        Console.WriteLine($"Degree: {student.Degree}");
                    }
                    else if (typeof(T) == typeof(Instructor))
                    {
                        Console.WriteLine($"Instructor ID: {(entity as Instructor).InstructorID}");
                    }
                }
                else
                {
                    Console.WriteLine($"{typeof(T).Name} not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error viewing {typeof(T).Name.ToLower()} details: {ex.Message}");
            }
        }

        static void DeleteEntity<T>(IRepository<T> repository) where T : Person
        {
            try
            {
                Console.Write($"Enter {typeof(T).Name} ID to delete: ");
                string id = Console.ReadLine();
                repository.Delete(id);
                Console.WriteLine($"{typeof(T).Name} Deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting {typeof(T).Name.ToLower()}: {ex.Message}");
            }
        }

        static void AddSemesterAndCourses(IRepository<Student> studentRepository)
        {
            try
            {
                Console.Write("Enter Student ID: ");
                string studentId = Console.ReadLine();
                Student student = studentRepository.GetById(studentId);
                if (student != null)
                {
                    Console.WriteLine("Enter Semester Information:");
                    Console.Write("Semester Code (e.g., Spring, Summer, Fall): ");
                    string semesterCode = Console.ReadLine();
                    Console.Write("Year: ");
                    string year = Console.ReadLine();
                    string newSemester = $"{semesterCode} {year}";

                    // Retrieve all available courses
                    List<Course> allCourses = GetAllCourses();

                    // Determine courses the student has not taken yet
                    var coursesNotTaken = allCourses.Where(course => !student.CoursesInSemester.Any(c => c.CourseID == course.CourseID));

                    // Display courses the student has not taken yet
                    Console.WriteLine("Courses not taken yet:");
                    foreach (var course in coursesNotTaken)
                    {
                        Console.WriteLine($"{course.CourseID}: {course.CourseName}");
                    }

                    // Select courses to add to the semester
                    Console.WriteLine("Select courses to add to the semester (enter course IDs separated by comma):");
                    string selectedCourseIdsInput = Console.ReadLine();
                    string[] selectedCourseIds = selectedCourseIdsInput.Split(',');

                    // Add selected courses to the semester
                    foreach (var courseId in selectedCourseIds)
                    {
                        Course selectedCourse = allCourses.FirstOrDefault(course => course.CourseID == courseId.Trim());
                        if (selectedCourse != null)
                        {
                            student.CoursesInSemester.Add(selectedCourse);
                            Console.WriteLine($"Added course {selectedCourse.CourseID} to {newSemester}.");
                        }
                        else
                        {
                            Console.WriteLine($"Course with ID {courseId} not found.");
                        }
                    }

                    studentRepository.SaveChanges();
                    Console.WriteLine("Semester and courses added successfully.");
                }
                else
                {
                    Console.WriteLine("Student not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding semester and courses: {ex.Message}");
            }
        }


        static void AddCoursesToSemester(Student student, string semester, params string[] courseIds)
        {
            foreach (var courseId in courseIds)
            {
                Course selectedCourse = GetAllCourses().GetCourseById(courseId.Trim());
                if (selectedCourse != null)
                {
                    student.CoursesInSemester.Add(selectedCourse);
                    Console.WriteLine($"Added course {selectedCourse.CourseID} to {semester}.");
                }
                else
                {
                    Console.WriteLine($"Course with ID {courseId} not found.");
                }
            }
        }

        static void DisplayStudentList(IRepository<Student> studentRepository)
        {
            try
            {
                var studentRepo = studentRepository as JsonRepository<Student>;
                var students = studentRepo?.GetAllEntities();

                if (students != null && students.Any())
                {
                    Console.WriteLine("Student List:");
                    foreach (var student in students)
                    {
                        Console.WriteLine($"Name: {student.FirstName} {student.MiddleName} {student.LastName}, ID: {student.StudentID}");
                    }
                }
                else
                {
                    Console.WriteLine("No students found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying student list: {ex.Message}");
            }
        }



        static List<Course> GetAllCourses()
        {
        
            List<Course> courses = new List<Course>
            {
                new Course { CourseID = "CSC101", CourseName = "Introduction to Computer Science", InstructorName = "Dr. Smith", NumberOfCredits = 3 },
                new Course { CourseID = "ENG201", CourseName = "English Literature", InstructorName = "Prof. Johnson", NumberOfCredits = 4 },
                new Course { CourseID = "BBA301", CourseName = "Business Management", InstructorName = "Mr. Brown", NumberOfCredits = 3 }
          
            };
            return courses;
        }
    }
}
