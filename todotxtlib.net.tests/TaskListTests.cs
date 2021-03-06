﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace todotxtlib.net.tests
{
    [TestFixture]
	internal class TaskListTests
	{
		private string _testDataPath = "testtasks.txt";

		private string CreateTempTasksFile()
		{
			string tempTaskFile = Path.GetRandomFileName();
			File.Copy(_testDataPath, tempTaskFile, true);
			return tempTaskFile;
		}

		[Test]
		public void SelectMany()
		{
			string tempTaskFile = CreateTempTasksFile();
			var tl = new TaskList(tempTaskFile);
			IOrderedEnumerable<string> contexts = tl.SelectMany(task => task.Contexts,
			                                                    (task, context) => context).Distinct().OrderBy(context => context);

			Assert.AreEqual(3, contexts.Count());
		}

		[Test]
		public void Add_Multiple()
		{
			var tl = new TaskList(_testDataPath);
			int c = tl.Count();

			var task = new Task("Add_Multiple task one");
			tl.Add(task);

			var task2 = new Task("Add_Multiple task two");
			tl.Add(task2);

			Assert.AreEqual(c + 2, tl.Count());
		}

		[Test]
		public void Add_ToCollection()
		{
			var task = new Task("(B) Add_ToCollection +test @task");

			var tl = new TaskList(_testDataPath);

			List<Task> tasks = tl.ToList();
			tasks.Add(task);

			tl.Add(task);

			List<Task> newTasks = tl.ToList();

			Assert.AreEqual(tasks.Count, newTasks.Count);

			for (int i = 0; i < tasks.Count; i++)
				Assert.AreEqual(tasks[i].ToString(), newTasks[i].ToString());
		}

		[Test]
		public void Add_ToFile()
		{
			// Create a copy of test data so we can leave the original alone
			string tempTaskFile = CreateTempTasksFile();

			List<string> fileContents = File.ReadAllLines(tempTaskFile).ToList();
			fileContents.Add("(B) Add_ToFile +test @task");

			var task = new Task(fileContents.Last());
			var tl = new TaskList(tempTaskFile);
			tl.Add(task);
			tl.SaveTasks(tempTaskFile);

			string[] newFileContents = File.ReadAllLines(tempTaskFile);
			CollectionAssert.AreEquivalent(fileContents, newFileContents);

			// Clean up
			File.Delete(tempTaskFile);
		}

        [Test]
        public void BlankLinesAreEmptyTasks()
        {
            // Create a copy of test data so we can leave the original alone
            string tempTaskFile = CreateTempTasksFile();

            var tl = new TaskList(tempTaskFile);
            var originalCount = tl.Count;

            File.AppendAllText(tempTaskFile, Environment.NewLine + Environment.NewLine + "The above line was blank" + Environment.NewLine);

            var st = File.Open(tempTaskFile, FileMode.Open);

            st.Close();

            var tl2 = new TaskList(tempTaskFile);

            Assert.AreEqual(originalCount + 2, tl2.Count, "Added two lines, one of which was blank (empty)");
        }

	    [Test]
		public void Add_To_Empty_File()
		{
			// v0.3 and earlier contained a bug where a blank task was added
			string tempTaskFile = CreateTempTasksFile();
			File.WriteAllLines(tempTaskFile, new string[] {}); // empties the file

			var tl = new TaskList(tempTaskFile);
			tl.Add(new Task("A task"));

			Assert.AreEqual(1, tl.Count());

			// Clean up
			File.Delete(tempTaskFile);
		}

		[Test]
		public void Construct()
		{
			var tl = new TaskList(_testDataPath);
		}

		[Test]
		public void Delete_InCollection()
		{
			var task = new Task("(B) Delete_InCollection +test @task");
			var tl = new TaskList(_testDataPath);
			tl.Add(task);

			List<Task> tasks = tl.ToList();
			tasks.Remove(tasks.Last());

			tl.Delete(task);

			List<Task> newTasks = tl.ToList();

			Assert.AreEqual(tasks.Count, newTasks.Count);

			for (int i = 0; i < tasks.Count; i++)
				Assert.AreEqual(tasks[i].ToString(), newTasks[i].ToString());
		}

		[Test]
		public void Delete_InFile()
		{
			string tempTasksFile = CreateTempTasksFile();
			try
			{
				string[] fileLines = File.ReadAllLines(tempTasksFile);
				List<string> fileContents = fileLines.ToList();
				var task = new Task(fileContents.Last());
				fileContents.Remove(fileContents.Last());

				var tl = new TaskList(tempTasksFile);
				tl.Delete(task);
				tl.SaveTasks(tempTasksFile);

				string[] newFileContents = File.ReadAllLines(tempTasksFile);
				CollectionAssert.AreEquivalent(fileContents, newFileContents);
			}
			finally
			{
				File.Delete(tempTasksFile);
			}
		}

        [Test]
        public void Load_From_File()
        {
            var tl = new TaskList(_testDataPath);
            IEnumerable<Task> tasks = tl.AsEnumerable();
        }

		[Test]
		public void Load_From_Stream_Repeated()
		{
			var s = new Stopwatch();

			s.Start();
			for (int n = 0; n < 500; n++)
			{
				using (FileStream fs = File.OpenRead(_testDataPath))
				{
					var tl = new TaskList();

					tl.LoadTasks(fs);
				}
			}
			s.Stop();

			Debug.WriteLine(s.Elapsed);
		}

		[Test]
		public void Load_From_Stream()
		{
			using (FileStream fs = File.OpenRead(_testDataPath))
			{
				var tl = new TaskList();

				tl.LoadTasks(fs);

				Assert.AreEqual(8, tl.Count);
			}
		}

		[Test]
		public void ObservableChanges()
		{
			var tl = new TaskList();

			bool fired = false;

			tl.CollectionChanged += (sender, e) => { fired = true; };

			tl.LoadTasks(_testDataPath);

			Assert.True(fired);
			fired = false;

			tl.Add(new Task("T", null, null, "Test task for observablecollection event firing"));

			Assert.True(fired);
			fired = false;

			tl[0].PropertyChanged += (sender, e) => { fired = true; };

			tl[0].Append("Test append for propertychanged event firing");

			Assert.True(fired);
		}

		[Test]
		public void Save_To_Stream()
		{
			string tempTaskFile = CreateTempTasksFile();

			var tl = new TaskList();

			using (FileStream fs = File.OpenRead(tempTaskFile))
			{
				tl.LoadTasks(fs);
			}

			tl.Add(new Task("This task should end up in both lists"));

			string tempTaskFileCopy = CreateTempTasksFile();

			using (FileStream fs = File.OpenWrite(tempTaskFileCopy))
			{
				tl.SaveTasks(fs);
			}

			var tl2 = new TaskList(tempTaskFileCopy);

			Assert.AreEqual(tl.Count, tl2.Count);
		}

		[Test]
		public void Search()
		{
			string tempTasksFile = CreateTempTasksFile();
			var tl = new TaskList(tempTasksFile);

			// There should be two task which contain the term 'foo'
			TaskList fooTaskList = tl.Search("foo");
			Assert.IsNotNull(fooTaskList);
			Assert.AreEqual(2, fooTaskList.Count);

			// Search should be case insenstive
			TaskList caseInsensitiveTaskList = tl.Search("Foo");
			Assert.IsNotNull(caseInsensitiveTaskList);
			Assert.AreEqual(2, caseInsensitiveTaskList.Count);

			// '-' in front of the term should find all tasks without the term
			TaskList notFooTaskList = tl.Search("-foo");
			// So searching the list generated by the negative search for the term
			// should give us an empty list
			Assert.AreEqual(0, notFooTaskList.Search("foo").Count);
		}

		[Test]
		public void ToggleComplete_Off_InCollection()
		{
			// Not complete - doesn't include completed date
			var task = new Task("X (B) ToggleComplete_Off_InCollection +test @task");
			var tl = new TaskList(_testDataPath);
			tl.Add(task);

			task = tl.Last();

			task.ToggleCompleted();

			task = tl.Last();

			Assert.IsTrue(task.Completed);

			var task2 = new Task("X 2011-02-25 ToggleComplete_Off_InCollection +test @task");

			tl.Add(task2);

			task = tl.Last();

			task.ToggleCompleted();

			task = tl.Last();

			Assert.IsFalse(task.Completed);
		}

		[Test]
		public void ToggleComplete_On_InCollection()
		{
			var task = new Task("(B ToggleComplete_On_InCollection +test @task");
			var tl = new TaskList(_testDataPath);
			tl.Add(task);

			task = tl.Last();

			task.ToggleCompleted();

			task = tl.Last();

			Assert.IsTrue(task.Completed);
		}

		[Test]
		public void Update_InCollection()
		{
			var task = new Task("(B) Update_InCollection +test @task");

			var tl = new TaskList(_testDataPath);
			tl.Add(task);

			var task2 = new Task(task.Raw);
			task2.ToggleCompleted();

			tl.Update(task, task2);

			Task newTask = tl.Last();
			Assert.IsTrue(newTask.Completed);
		}

        [Test]
        public void LoadTasksFromString()
        {
            var text = @"
this is the first task
this is the second task

the previous line was blank";

            var tl = new TaskList();
            tl.LoadTasksFromString(text);

            Assert.That(tl.Count == 5);
            Assert.That(tl.Search("previous").Any());
        }
	}
}