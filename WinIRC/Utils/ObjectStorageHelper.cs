using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WinIRC.Utils
{
    public enum StorageType
    {
        Roaming, Local, Temporary
    }

    public class ObjectStorageHelper<T>
    {
        private ApplicationData appData = Windows.Storage.ApplicationData.Current;
        private XmlSerializer serializer;
        private StorageType storageType;

        private string FileName(T Obj, string Handle)
        {
            /* 
             * When I first started writing this class I thought I would only be storing objects that could be serialized as XML 
             * and hence I put a .xml suffix on the filename. I now realise the folly of doing this as I may want to, in the future,
             * store objects that cannot be serialized as XML (e.g. Media objects) and instead use some alternative method that does 
             * not involve serialization. 
             * I had a choice between supporting backward compatibility or dropping the .xml suffix. I opted for the latter.
             */

            var str = String.Concat(Handle, String.Format("{0}", Obj.GetType().ToString()));
            return str;
            //return String.Format("{0}.xml", Obj.GetType().FullName).Replace("'","").Replace(",","").Replace(".","").Replace("&","");
        }

        /// <summary>
        /// Generic class to simplify the storage and retrieval of data to/from Windows.Storage.ApplicationData
        /// </summary>
        /// <param name="StorageType"></param>
        public ObjectStorageHelper(StorageType StorageType)
        {
            serializer = new XmlSerializer(typeof(T));
            storageType = StorageType;
        }

        /// <summary>
        /// Delete a stored instance of T from Windows.Storage.ApplicationData
        /// </summary>
        /// <returns></returns>
        public async Task DeleteAsync()
        {
            string fileName = FileName(Activator.CreateInstance<T>(), String.Empty);
            await DeleteAsync(fileName);
        }
        /// <summary>
        /// Delete a stored instance of T with a specified handle from Windows.Storage.ApplicationData.
        /// Specification of a handle supports storage and deletion of different instances of T.
        /// </summary>
        /// <param name="Handle">User-defined handle for the stored object</param>
        public async Task DeleteAsync(string Handle)
        {
            if (Handle == null)
                throw new ArgumentNullException("Handle");
            string fileName = FileName(Activator.CreateInstance<T>(), Handle);
            try
            {
                StorageFolder folder = GetFolder(storageType);

                var file = await GetFileIfExistsAsync(folder, fileName);
                if (file != null)
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Store an instance of T to Windows.Storage.ApplicationData with a specified handle.
        /// Specification of a handle supports storage and deletion of different instances of T.
        /// </summary>
        /// <param name="Obj">Object to be saved</param>
        /// <param name="Handle">User-defined handle for the stored object</param>
        public async Task SaveAsync(T Obj, string Handle)
        {
            if (Obj == null)
                throw new ArgumentNullException("Obj");
            if (Handle == null)
                throw new ArgumentNullException("Handle");

            StorageFile file = null;
            string fileName = Handle;
            StorageFolder folder = GetFolder(storageType);
            file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            IRandomAccessStream writeStream = await file.OpenAsync(FileAccessMode.ReadWrite);

            using (Stream outStream = Task.Run(() => writeStream.AsStreamForWrite()).Result)
            {
                serializer.Serialize(outStream, Obj);
                await outStream.FlushAsync();
            }

        }

        /// <summary>
        /// Store an instance of T to Windows.Storage.ApplicationData with a specified handle.
        /// Specification of a handle supports storage and deletion of different instances of T.
        /// </summary>
        /// <param name="Obj">Object to be saved</param>
        /// <param name="Handle">User-defined handle for the stored object</param>
        public async Task MigrateAsync(T Obj, string Handle)
        {
            if (Obj == null)
                throw new ArgumentNullException("Obj");
            if (Handle == null)
                throw new ArgumentNullException("Handle");

            StorageFile file = null;
            string fileName = Handle;
            StorageFolder folder = GetFolder(storageType);
            file = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);
            IRandomAccessStream writeStream = await file.OpenAsync(FileAccessMode.ReadWrite);

            using (Stream outStream = Task.Run(() => writeStream.AsStreamForWrite()).Result)
            {
                serializer.Serialize(outStream, Obj);
                await outStream.FlushAsync();
            }
        }

        /// <summary>
        /// Save an instance of T to Windows.Storage.ApplicationData.
        /// </summary>
        /// <param name="Obj">Object to be saved</param>
        /// <param name="Handle">User-defined handle for the stored object</param>
        public async Task SaveAsync(T Obj)
        {
            string fileName = FileName(Obj, String.Empty);
            await SaveAsync(Obj, fileName);
        }

        /// <summary>
        /// Retrieve a stored instance of T with a specified handle from Windows.Storage.ApplicationData.
        /// Specification of a handle supports storage and deletion of different instances of T.
        /// </summary>
        /// <param name="Handle">User-defined handle for the stored object</param>
        public async Task<T> LoadAsync(string Handle)
        {
            if (Handle == null)
                throw new ArgumentNullException("Handle");

            string fileName;
            if (Handle == FileName(Activator.CreateInstance<T>(), String.Empty))
                fileName = FileName(Activator.CreateInstance<T>(), Handle);
            else
                fileName = Handle;

            try
            {
                StorageFile file = null;
                StorageFolder folder = GetFolder(storageType);
                file = await folder.GetFileAsync(fileName);
                IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read);
                using (Stream inStream = Task.Run(() => readStream.AsStreamForRead()).Result)
                {
                    return (T)serializer.Deserialize(inStream);
                }
            }
            catch (FileNotFoundException)
            {
                //file not existing is perfectly valid so simply return the default 
                return default(T);
                //Interesting thread here: How to detect if a file exists (http://social.msdn.microsoft.com/Forums/en-US/winappswithcsharp/thread/1eb71a80-c59c-4146-aeb6-fefd69f4b4bb)
                //throw;
            }
            catch (Exception)
            {
                //Unable to load contents of file
                throw;
            }
        }
        /// <summary>
        /// Retrieve a stored instance of T from Windows.Storage.ApplicationData.
        /// </summary>
        public async Task<T> LoadAsync()
        {
            string fileName = FileName(Activator.CreateInstance<T>(), String.Empty);
            return await LoadAsync(fileName);
        }

        public StorageFolder GetFolder(StorageType storageType)
        {
            StorageFolder folder;
            switch (storageType)
            {
                case StorageType.Roaming:
                    folder = appData.RoamingFolder;
                    break;
                case StorageType.Local:
                    folder = appData.LocalFolder;
                    break;
                case StorageType.Temporary:
                    folder = appData.TemporaryFolder;
                    break;
                default:
                    throw new Exception(String.Format("Unknown StorageType: {0}", storageType));
            }
            return folder;
        }

        public async Task<StorageFile> GetFileIfExistsAsync(StorageFolder folder, string fileName)
        {
            try
            {
                return await folder.GetFileAsync(fileName);
            }
            catch
            {
                return null;
            }
        }

        public async Task<Boolean> FileExists(StorageFolder folder, string fileName)
        {
            try
            {
                await folder.GetFileAsync(fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
