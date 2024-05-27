using System.IO;
using System.Linq;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;

public static class ZipUtility
{
	#region ZipCallback

	public abstract class ZipCallback
	{
		/// <summary>
		/// 压缩单个文件或文件夹前执行的回调
		/// </summary>
		/// <param name="_entry"></param>
		/// <returns>如果返回true，则压缩文件或文件夹，反之则不压缩文件或文件夹</returns>
		public virtual bool OnPreZip(ZipEntry _entry)
		{
			return true;
		}

		/// <summary>
		/// 压缩单个文件或文件夹后执行的回调
		/// </summary>
		/// <param name="_entry"></param>
		public virtual void OnPostZip(ZipEntry _entry)
		{
		}

		/// <summary>
		/// 压缩执行完毕后的回调
		/// </summary>
		/// <param name="_result">true表示压缩成功，false表示压缩失败</param>
		public virtual void OnFinished(bool _result)
		{
		}
	}

	#endregion

	#region UnzipCallback

	public abstract class UnzipCallback
	{
		/// <summary>
		/// 解压单个文件或文件夹前执行的回调
		/// </summary>
		/// <param name="_entry"></param>
		/// <returns>如果返回true，则压缩文件或文件夹，反之则不压缩文件或文件夹</returns>
		public virtual bool OnPreUnzip(ZipEntry _entry)
		{
			return true;
		}

		/// <summary>
		/// 解压单个文件或文件夹后执行的回调
		/// </summary>
		/// <param name="_entry"></param>
		public virtual void OnPostUnzip(ZipEntry _entry)
		{
		}

		/// <summary>
		/// 解压执行完毕后的回调
		/// </summary>
		/// <param name="_result">true表示解压成功，false表示解压失败</param>
		public virtual void OnFinished(bool _result)
		{
		}
	}

	#endregion

	/// <summary>
	/// 压缩文件和文件夹
	/// </summary>
	/// <param name="fileOrDirectoryArray">文件夹路径和文件名</param>
	/// <param name="outputPathName">压缩后的输出路径文件名</param>
	/// <param name="_password">压缩密码</param>
	/// <param name="_zipCallback">ZipCallback对象，负责回调</param>
	/// <returns></returns>
	public static bool Zip(string[] fileOrDirectoryArray, string outputPathName, string _password = null,
		ZipCallback _zipCallback = null, string _parentRelPath = null)
	{
		if ((null == fileOrDirectoryArray) || string.IsNullOrEmpty(outputPathName))
		{
			if (null != _zipCallback)
				_zipCallback.OnFinished(false);

			return false;
		}

		ZipOutputStream zipOutputStream = new ZipOutputStream(File.Create(outputPathName));
		zipOutputStream.SetLevel(6); // 压缩质量和压缩速度的平衡点
		if (!string.IsNullOrEmpty(_password))
			zipOutputStream.Password = _password;

		for (int index = 0; index < fileOrDirectoryArray.Length; ++index)
		{
			bool result = false;
			string fileOrDirectory = fileOrDirectoryArray[index];
			string parentRelPath = string.Empty;
			if (_parentRelPath != null)
			{
				parentRelPath = fileOrDirectory.Substring(_parentRelPath.Length + 1);
				parentRelPath =
					parentRelPath.Substring(0, parentRelPath.Length - Path.GetFileName(fileOrDirectory).Length);
				if (!string.IsNullOrEmpty(parentRelPath))
				{
					parentRelPath = parentRelPath.Substring(0, parentRelPath.Length - 1);
				}
			}

			if (Directory.Exists(fileOrDirectory))
				result = ZipDirectory(fileOrDirectory, parentRelPath, zipOutputStream, _zipCallback);
			else if (File.Exists(fileOrDirectory))
				result = ZipFile(fileOrDirectory, parentRelPath, zipOutputStream, _zipCallback);

			if (!result)
			{
				if (null != _zipCallback)
					_zipCallback.OnFinished(false);

				return false;
			}
		}

		zipOutputStream.Finish();
		zipOutputStream.Close();

		if (null != _zipCallback)
			_zipCallback.OnFinished(true);

		return true;
	}
	
	/// <summary>
	/// 解压Zip包
	/// </summary>
	/// <param name="_filePathName">Zip包的文件路径名</param>
	/// <param name="_outputPath">解压输出路径</param>
	/// <param name="_password">解压密码</param>
	/// <param name="_unzipCallback">UnzipCallback对象，负责回调</param>
	/// <returns></returns>
	public static bool UnzipFile(string _filePathName, string _outputPath, string _password = null,
		UnzipCallback _unzipCallback = null)
	{
		if (string.IsNullOrEmpty(_filePathName) || string.IsNullOrEmpty(_outputPath))
		{
			if (null != _unzipCallback)
				_unzipCallback.OnFinished(false);

			return false;
		}

		try
		{
			return UnzipFile(File.OpenRead(_filePathName), _outputPath, _password, _unzipCallback);
		}
		catch (System.Exception _e)
		{
			Debug.LogError("[ZipUtility.UnzipFile]: " + _e.ToString());

			if (null != _unzipCallback)
				_unzipCallback.OnFinished(false);

			return false;
		}
	}

	/// <summary>
	/// 解压Zip包
	/// </summary>
	/// <param name="_fileBytes">Zip包字节数组</param>
	/// <param name="_outputPath">解压输出路径</param>
	/// <param name="_password">解压密码</param>
	/// <param name="_unzipCallback">UnzipCallback对象，负责回调</param>
	/// <returns></returns>
	public static bool UnzipFile(byte[] _fileBytes, string _outputPath, string _password = null,
		UnzipCallback _unzipCallback = null)
	{
		if ((null == _fileBytes) || string.IsNullOrEmpty(_outputPath))
		{
			if (null != _unzipCallback)
				_unzipCallback.OnFinished(false);

			return false;
		}

		bool result = UnzipFile(new MemoryStream(_fileBytes), _outputPath, _password, _unzipCallback);
		if (!result)
		{
			if (null != _unzipCallback)
				_unzipCallback.OnFinished(false);
		}

		return result;
	}

	/// <summary>
	/// 解压Zip包
	/// </summary>
	/// <param name="_inputStream">Zip包输入流</param>
	/// <param name="_outputPath">解压输出路径</param>
	/// <param name="_password">解压密码</param>
	/// <param name="_unzipCallback">UnzipCallback对象，负责回调</param>
	/// <returns></returns>
	public static bool UnzipFile(Stream _inputStream, string _outputPath, string _password = null,
		UnzipCallback _unzipCallback = null)
	{
		if ((null == _inputStream) || string.IsNullOrEmpty(_outputPath))
		{
			if (null != _unzipCallback)
				_unzipCallback.OnFinished(false);

			return false;
		}

		// 创建文件目录
		if (!Directory.Exists(_outputPath))
			Directory.CreateDirectory(_outputPath);

		// 解压Zip包
		ZipEntry entry = null;
		using (ZipInputStream zipInputStream = new ZipInputStream(_inputStream))
		{
			if (!string.IsNullOrEmpty(_password))
				zipInputStream.Password = _password;

			while (null != (entry = zipInputStream.GetNextEntry()))
			{
				if (string.IsNullOrEmpty(entry.Name))
					continue;

				if ((null != _unzipCallback) && !_unzipCallback.OnPreUnzip(entry))
					continue; // 过滤

				string filePathName = Path.Combine(_outputPath, entry.Name);

				// 创建文件目录
				if (entry.IsDirectory)
				{
					Directory.CreateDirectory(filePathName);
					continue;
				}

				// 写入文件
				try
				{
					using (FileStream fileStream = File.Create(filePathName))
					{
						byte[] bytes = new byte[1024];
						while (true)
						{
							int count = zipInputStream.Read(bytes, 0, bytes.Length);
							if (count > 0)
								fileStream.Write(bytes, 0, count);
							else
							{
								if (null != _unzipCallback)
									_unzipCallback.OnPostUnzip(entry);

								break;
							}
						}
					}
				}
				catch (System.Exception _e)
				{
					Debug.LogError("[ZipUtility.UnzipFile]: " + _e.ToString());

					if (null != _unzipCallback)
						_unzipCallback.OnFinished(false);

					return false;
				}
			}
		}

		if (null != _unzipCallback)
			_unzipCallback.OnFinished(true);

		return true;
	}

	/// <summary>
	/// 压缩文件
	/// </summary>
	/// <param name="filePathName">文件路径名</param>
	/// <param name="parentRelPath">要压缩的文件的父相对文件夹</param>
	/// <param name="zipOutputStream">压缩输出流</param>
	/// <param name="_zipCallback">ZipCallback对象，负责回调</param>
	/// <returns></returns>
	private static bool ZipFile(string filePathName, string parentRelPath, ZipOutputStream zipOutputStream,
		ZipCallback _zipCallback = null)
	{
		if (filePathName.EndsWith(".DS_Store"))
			return true;
		//Crc32 crc32 = new Crc32();
		ZipEntry entry = null;
		FileStream fileStream = null;
		try
		{
			string entryName = parentRelPath + '/' + Path.GetFileName(filePathName);
			entry = new ZipEntry(entryName);
			entry.DateTime = System.DateTime.Now;

			if ((null != _zipCallback) && !_zipCallback.OnPreZip(entry))
				return true; // 过滤

			fileStream = File.OpenRead(filePathName);
			byte[] buffer = new byte[fileStream.Length];
			fileStream.Read(buffer, 0, buffer.Length);
			fileStream.Close();

			entry.Size = buffer.Length;

			//crc32.Reset();
			//crc32.Update(buffer);
			//entry.Crc = crc32.Value;

			zipOutputStream.PutNextEntry(entry);
			zipOutputStream.Write(buffer, 0, buffer.Length);
		}
		catch (System.Exception _e)
		{
			Debug.LogError("[ZipUtility.ZipFile]: " + _e.ToString());
			return false;
		}
		finally
		{
			if (null != fileStream)
			{
				fileStream.Close();
				fileStream.Dispose();
			}
		}

		if (null != _zipCallback)
			_zipCallback.OnPostZip(entry);

		return true;
	}

	/// <summary>
	/// 压缩文件夹
	/// </summary>
	/// <param name="path">要压缩的文件夹</param>
	/// <param name="parentRelPath">要压缩的文件夹的父相对文件夹</param>
	/// <param name="zipOutputStream">压缩输出流</param>
	/// <param name="_zipCallback">ZipCallback对象，负责回调</param>
	/// <returns></returns>
	private static bool ZipDirectory(string path, string parentRelPath, ZipOutputStream zipOutputStream,
		ZipCallback _zipCallback = null)
	{
		ZipEntry entry = null;
		try
		{
			string entryName = Path.Combine(parentRelPath, Path.GetFileName(path) + '/');
			entry = new ZipEntry(entryName);
			entry.DateTime = System.DateTime.Now;
			entry.Size = 0;

			if ((null != _zipCallback) && !_zipCallback.OnPreZip(entry))
				return true; // 过滤

			zipOutputStream.PutNextEntry(entry);
			zipOutputStream.Flush();

			string[] files = Directory.GetFiles(path);
			for (int index = 0; index < files.Length; ++index)
				ZipFile(files[index], Path.Combine(parentRelPath, Path.GetFileName(path)), zipOutputStream,
					_zipCallback);
		}
		catch (System.Exception _e)
		{
			Debug.LogError("[ZipUtility.ZipDirectory]: " + _e.ToString());
			return false;
		}

		string[] directories = Directory.GetDirectories(path);
		for (int index = 0; index < directories.Length; ++index)
		{
			if (!ZipDirectory(directories[index], Path.Combine(parentRelPath, Path.GetFileName(path)),
				    zipOutputStream, _zipCallback))
				return false;
		}

		if (null != _zipCallback)
			_zipCallback.OnPostZip(entry);

		return true;
	}
	
	public static bool Zip(string[] fileOrDirectoryArray, string outputPathName, string[] fileFliter)
	{
		if ((null == fileOrDirectoryArray) || string.IsNullOrEmpty(outputPathName))
		{
			return false;
		}

		ZipOutputStream zipOutputStream = new ZipOutputStream(File.Create(outputPathName));
		zipOutputStream.SetLevel(6); // 压缩质量和压缩速度的平衡点

		for (int index = 0; index < fileOrDirectoryArray.Length; ++index)
		{
			bool result = false;
			string fileOrDirectory = fileOrDirectoryArray[index];
			string parentRelPath = string.Empty;
			
			if (Directory.Exists(fileOrDirectory))
				result = ZipDirectory(fileOrDirectory, parentRelPath, zipOutputStream, fileFliter);
			else if (File.Exists(fileOrDirectory))
				result = ZipFile(fileOrDirectory, parentRelPath, zipOutputStream, fileFliter);

			if (!result)
			{
				return false;
			}
		}

		zipOutputStream.Finish();
		zipOutputStream.Close();
		return true;
	}
	
	private static bool ZipDirectory(string path, string parentRelPath, ZipOutputStream zipOutputStream, string[] fileFliter)
	{
		ZipEntry entry = null;
		try
		{
			string entryName = Path.Combine(parentRelPath, Path.GetFileName(path) + '/');
			entry = new ZipEntry(entryName);
			entry.DateTime = System.DateTime.Now;
			entry.Size = 0;
			
			zipOutputStream.PutNextEntry(entry);
			zipOutputStream.Flush();
			string[] files = Directory.GetFiles(path);
			for (int index = 0; index < files.Length; ++index)
				ZipFile(files[index].Replace("\\", "/"), Path.Combine(parentRelPath, Path.GetFileName(path)), zipOutputStream,
					fileFliter);
		}
		catch (System.Exception _e)
		{
			Debug.LogError("[ZipUtility.ZipDirectory]: " + _e.ToString());
			return false;
		}

		string[] directories = Directory.GetDirectories(path);
		for (int index = 0; index < directories.Length; ++index)
		{
			var childPath = directories[index].Replace("\\", "/");
			if (fileFliter != null && fileFliter.Contains(childPath))
				continue;
			if (!ZipDirectory(childPath, Path.Combine(parentRelPath, Path.GetFileName(path)),
				    zipOutputStream, fileFliter))
				return false;
		}

		return true;
	}
	
	private static bool ZipFile(string filePathName, string parentRelPath, ZipOutputStream zipOutputStream, string[] fileFliter)
	{
		if (filePathName.EndsWith(".DS_Store") || (fileFliter != null && fileFliter.Contains(filePathName)) )
			return true;
		//Crc32 crc32 = new Crc32();
		ZipEntry entry = null;
		FileStream fileStream = null;
		try
		{
			string entryName = parentRelPath + '/' + Path.GetFileName(filePathName);
			entry = new ZipEntry(entryName);
			entry.DateTime = System.DateTime.Now;
			
			fileStream = File.OpenRead(filePathName);
			byte[] buffer = new byte[fileStream.Length];
			fileStream.Read(buffer, 0, buffer.Length);
			fileStream.Close();

			entry.Size = buffer.Length;

			//crc32.Reset();
			//crc32.Update(buffer);
			//entry.Crc = crc32.Value;

			zipOutputStream.PutNextEntry(entry);
			zipOutputStream.Write(buffer, 0, buffer.Length);
		}
		catch (System.Exception _e)
		{
			Debug.LogError("[ZipUtility.ZipFile]: " + _e.ToString());
			return false;
		}
		finally
		{
			if (null != fileStream)
			{
				fileStream.Close();
				fileStream.Dispose();
			}
		}
		
		return true;
	}
}