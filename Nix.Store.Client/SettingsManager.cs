using log4net;
using Nix.Store.Client.Engine;
using Nix.Store.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Nix.Store.Client
{
    /// <summary>
    /// Contenedor de configuraciones estandar.
    /// Permite el almacenamiento de forma concurrente de las configuraciones de varias aplicaciones conectadas.
    /// Se pueden obtener los diferentes contenedores a traves de sus metodos estaticos.
    /// Por convencion se usa nombre de librerias como llaves para cada contenedor. 
    /// Cada contenedor esta formado por Grupos, cada grupo tiene una llave. Esta puede ser repetida en diferentes contenedores pero no en el mismo.
    /// Todos los datos que contiene ya vienen deserializados en sus tipos respectivos invocando <see cref="Get(string, string)"/>.   
    /// Se puede añadir un contenedor o configuraciones en timepo de ejecuccion invocando <see cref="Set{T}(string, string, T)"/>.
    /// Todos los contenedores se puede refrescar en tiempo de ejecuccion, siempre y cuando tengas asociada un fuente de datos.
    /// Los contenedores se cargan a partir de uno o varios <see cref="ILoader"/>. Por ejemplo un contenedor puede tener una fuente de datos independiente por grupo. 
    /// Eso pertmite, por ejemplo, cargar un grupo desde un fichero local y los demas desde el servicio remoto.
    /// </summary>
    /// <seealso cref="GetAppStore(object)"/>
    /// <seealso cref="GetStore(string)"/>
    public sealed class SettingsManager
    {
        private static ILog Logger = LogManager.GetLogger(typeof(SettingsManager).Name);
        private static object _instanceLock = new object();

        #region MultiStore Implementation

        // Conjunto de metodos para el control estatico de los contenedores de configuraciones.
        // Cada objeto puede pedir acceso independiente a un contenedor concreto, o al por defecto, este es el que tiene como llave le nombre de su libreria.

        private static Dictionary<string, SettingsManager> _stores = new Dictionary<string, SettingsManager>();     //Contenedores        

        #region Getters

        /// <summary>
        /// Devuelve el contenedor con el nombre de <see cref="System.Assembly"/> del objeto que llama. 
        /// Se considera que ese contenedor es el por defecto para el objeto que peticiona.
        /// </summary>
        /// <param name="requestor">Objeto que demanda su contenedor por defecto.</param>
        /// <returns><see cref="SettingsManager"/> peticionado.</returns>
        /// <exception cref="StoreNotFoundException">No se ha encontrado el contenedor.</exception><see cref="SettingsManager"/> peticionado.
        public static SettingsManager GetAppStore(object requestor)
        {
            var appName = requestor.GetType().Assembly.GetName().Name;
            return GetStore(appName);
        }
        /// <summary>
        /// Devuelve un contenedor a partir de un nombre.
        /// </summary>
        /// <param name="appKey">Nombre del contenedor</param>
        /// <returns><see cref="SettingsManager"/> peticionado.</returns>
        /// <exception cref="StoreNotFoundException">No se ha encontrado el contenedor.</exception><see cref="SettingsManager"/> peticionado.
        public static SettingsManager GetStore(string appKey)
        {
            if (String.IsNullOrWhiteSpace(appKey))
                throw new ArgumentException("AppKey must be not null and not empty", "appKey");

            lock (_instanceLock)
            {                
                SettingsManager store;
                if (_stores.TryGetValue(appKey, out store))
                    return store;
                else
                    throw new StoreNotFoundException(appKey);
            }
        }        

        #endregion

        #region Meta

        public static int GetStoresCount()
        {
            return _stores.Count;
        }

        public static List<string> GetStroresKeys()
        {
            return new List<string>(_stores.Keys);
        }

        public static List<string> GetGroupKeys(string appKey)
        {
            return new List<string>(SettingsManager.GetStore(appKey)._settingsContainer.Keys);
        }

        #endregion

        #region Gestion de contenido

        /// <summary>
        /// Carga un contenedor a partir de un cargador con una fuente de datos.
        /// En caso de existir un contenedor con la misma llave se mezclan ambas fuentes.
        /// </summary>
        /// <param name="appKey">Llave de la aplicacion a cargar.</param>
        /// <param name="loader">Cargador de datos.</param>        
        /// <remarks>Este metodo se invoca al leer las fuentes en un app.config o web.config. ver <see cref="ConfigurationLoader"/>.</remarks>
        /// <seealso cref="MergeSettings(ConcurrentDictionary{String, IDictionary})"/>
        public static void LoadStore(string appKey, ILoader loader)
        {
            lock (_instanceLock)
            {
                if (_stores.ContainsKey(appKey))
                {
                    var store = _stores[appKey];
                    store.MergeSettings(loader.Load());
                    store._loaders.Add(loader);
                }
                else
                {
                    _stores.Add(appKey, new SettingsManager(appKey, loader.Load()));
                    _stores[appKey]._loaders.Add(loader);
                }  
            }
        }

        /// <summary>
        /// Recarga estaticamente todas las configuraciones desde sus fuentes.
        /// </summary>
        /// <param name="onlyRemote">Esteblece si solo se actualizaran contenedores con fuentes externas.</param>
        public static void ReloadStores(bool onlyRemote = true)
        {
            lock (_instanceLock)
            {                
                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3 };
                Parallel.ForEach(_stores, parallelOptions,
                    (store) => store.Value.RefreshConfig(onlyRemote));                
            }
        }

        #endregion        

        #region Manual-Mode

        // Metodos para Crear y destruir configuraciones dinamicas en tiempo de ejecuccion.
        // Se puede usar estos metodos a modo de cache seguro dentro de las aplicaciones.

        /// <summary>
        /// Crea un contenedor en modo manual y los guarda en el sistema de configuracion. 
        /// El programador puede crear un contenedor en tiempo de ejecuccion y usarlo posteriormente con los demas contenedores.
        /// </summary>
        /// <param name="appKey">Llave para el contenedor. No puede estar repetida.</param>
        /// <param name="groups">Listado de nombres para los grupos. Debe tener almenos uno.</param>
        /// <returns>Contenedor creado.</returns>
        public static SettingsManager CreateContainer(string appKey, IEnumerable<string> groups)
        {            
            lock (_instanceLock)
            {
                //Validaciones
                if (String.IsNullOrWhiteSpace(appKey))
                    throw new ArgumentException("AppKey must be not null and not empty", "appKey");
                if (groups == null)
                    throw new ArgumentNullException("groups", "Groups cannot be null.");
                if (!groups.Any())
                    throw new ArgumentException("Groups must have at least 1 element.", "groups");
                if (groups.Any(x => String.IsNullOrWhiteSpace(x)))
                    throw new ArgumentException("All groups must be not null and not empty.", "groups");                
                if (_stores.ContainsKey(appKey))
                    throw new DuplicateKeyException(appKey, String.Format("Cannot create SettingsManager with key {0}, call method MergeSettings instead.", appKey));

                //Crear contenedor
                var storeData = new ConcurrentDictionary<string, IDictionary<string, object>>(
                        groups.Select(groupKey => 
                            new KeyValuePair<string, IDictionary<string, object>>(
                                groupKey, 
                                new ConcurrentDictionary<string, object>()
                                )));
                var newStore = new SettingsManager(appKey, storeData);
                foreach (var group in groups)
                    newStore._loaders.Add(LoaderFactory.GetLoader(Sections.LoaderType.Manual, appKey, null, group));
                _stores.Add(appKey, newStore);
                Logger.InfoFormat("[Manual-Mode] {0} SettingsManager Store Added successfully.", appKey);
                return newStore;
            }
        }

        public static void AddGroup(string appKey, string group)
        {
            lock (_instanceLock)
            {               
                if (String.IsNullOrWhiteSpace(group))
                    throw new ArgumentException("Group must be not null and not empty", "group");
                var store = GetStore(appKey);   //Get the Store
                if (store._settingsContainer.ContainsKey(group))
                    throw new DuplicateKeyException(group, String.Format("Group {0} already exisit in Store {1}.", group, appKey));
                if (!store._settingsContainer.TryAdd(group, new ConcurrentDictionary<string, object>()))
                    throw new Exception(String.Format("Error adding Group {0} into Store {1}", group, appKey));
                store._loaders.Add(LoaderFactory.GetLoader(Sections.LoaderType.Manual, appKey, null, group));
            }
        }

        public static void RemoveGroup(string appKey, string group)
        {
            lock (_instanceLock)
            {                
                if (String.IsNullOrWhiteSpace(group))
                    throw new ArgumentException("Group must be not null and not empty", "group");
                var store = GetStore(appKey);   //Get the Store
                IDictionary<string, object> innerGroup;
                if (store._settingsContainer.TryRemove(group, out innerGroup))
                {
                    var loader = store._loaders.FirstOrDefault(x => x.AppKey == appKey && x.Group == group);
                    if (loader != null)
                        store._loaders.Remove(loader);                    
                }
                else                
                    throw new Exception(String.Format("Error removing Group {0} at Store {1}", appKey, group));
                
            }
        }

        #endregion

        #endregion

        #region Constructor

        private SettingsManager(string appKey, ConcurrentDictionary<string, IDictionary<string, object>> storeData)
        {            
            this.InitializeContainer(appKey, storeData);
        }

        #endregion

        #region Container Management

        // Gestion del contenedor como objeto (accesos no estaticos)

        /// <summary>
        /// Nombre del contenedor de configuraciones.
        /// </summary>
        /// <remarks>Por defecto se usa el nombre de <see cref="System.Assembly"/> de cada modulo.</remarks>
        public string AppKey { get; private set; }

        /// <summary>
        /// Diccionario interno con los grupos y valores del contenedor.
        /// </summary>
        private ConcurrentDictionary<string, IDictionary<string, object>> _settingsContainer = null;
        private bool _throwsExceptionOnNotFound = false;

        /// <summary>
        /// Lista de cargadores de fuentes. 
        /// En caso de <see cref="RefreshConfig(bool)"/> se vuelven a llamar las fuentes que contienen para realimentar las configuraciones.
        /// </summary>
        /// <seealso cref="XmlFileLoader"/>
        /// <seealso cref="RemoteWebLoader"/>
        /// <seealso cref="ManualLoader"/>
        private List<ILoader> _loaders = new List<ILoader>();       

        private void InitializeContainer(string appKey, ConcurrentDictionary<string, IDictionary<string, object>> storeData)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(appKey))
                    throw new ArgumentException("AppKey cannot be null or empty.", "appKey");
                this.AppKey = appKey;
                _settingsContainer = storeData;
            }
            catch (Exception ex)
            {
                throw new InitializationExcpetion(ex);
            }
        }     

        /// <summary>
        /// Recarga la configuracion del contenedor volviendo a cargar las fuentes y haciendo un <see cref="MergeSettings(ConcurrentDictionary{String, IDictionary{String, Object}})"/>
        /// </summary>
        /// <param name="onlyRemote">Indica que solo se acuualizaran las fuentes de datos remotas. Por defecto es verdadero.</param>
        /// <remarks>Los origenes en Modo manual no se recargan nunca.</remarks>
        public void RefreshConfig(bool onlyRemote = true)
        {
            lock (_instanceLock)
            {
                foreach (var loader in _loaders)
                {
                    try
                    {
                        if (loader.LoaderType == Sections.LoaderType.Manual)    // Los cargadores Manuales se evitan, ya que se han generado en Run-time.
                            continue;
                        if (loader.LoaderType == Sections.LoaderType.RemoteWeb || !onlyRemote)
                            this.MergeSettings(loader.Load());
                    }
                    catch (Exception ex)
                    {
                        Logger.WarnFormat("Failed to Refresh Config for {0} with {1} Loader Type. Details: {2}", loader.AppKey, loader.LoaderType, ex.ToString());                            
                    }                    
                }
            }
        }

        /// <summary>
        /// Mezcla configuraciones obtenidas desde una fuente con las actuales.
        /// En caso de conflicto se actualiza el valor que viene del nuevo origen de datos.
        /// </summary>
        /// <param name="source"></param>
        public void MergeSettings(ConcurrentDictionary<string, IDictionary<string, object>> source)
        {
            lock (_instanceLock)
            {
                foreach (var item in source)
                {
                    if (!this._settingsContainer.ContainsKey(item.Key))                    
                        this._settingsContainer.TryAdd(item.Key, item.Value);                    
                    else                    
                        foreach (var setting in item.Value)
                        {
                            if (!this._settingsContainer[item.Key].ContainsKey(setting.Key))
                                this._settingsContainer[item.Key].Add(setting);
                            else
                            {
                                this._settingsContainer[item.Key][setting.Key] = setting.Value;
                                Logger.InfoFormat("XmlSetting -> {0}-{1} value updated from a Merge", item.Key.ToString(), setting.Key);
                            }
                        }
                    
                }
            }
        }             

        private bool InnerContainsKey(string group, string key)
        {
            return _settingsContainer[group].ContainsKey(key);
        }

        #endregion

        #region Specialized Bag Properties

        private bool InnerGet(string group, string key, out string result)
        {
            return this.InnerGet<string>(group, key, out result);
        }

        private bool InnerGet<T>(string group, string key, out T result)
        {      
            IDictionary<string, object> bagContainer;
            if (!_settingsContainer.TryGetValue(group, out bagContainer))
                throw new KeyNotFoundException(String.Format("Group {0} not found into Settings container {2}", group, this.AppKey)); 
            if (bagContainer.ContainsKey(key))
            {
                object unCastedResult = bagContainer[key];
                if (unCastedResult is LazyDeserializeObject)    // Deserializacion en destino, esto ocurre cuando el servidor no tenia el tipo para deserializar.
                {
                    var lazyUnCastedResult = ((LazyDeserializeObject)unCastedResult).Deserialize();
                    if (lazyUnCastedResult is T)
                    {
                        bagContainer[key] = lazyUnCastedResult;
                        result = (T)lazyUnCastedResult;
                        return true;
                    }
                }
                if (unCastedResult is T)
                    result = (T)unCastedResult;
                else
                    throw new InvalidCastException(String.Format(
                        "Invalid Cast Type on Setting {0} in Group {1} into Store {2}, expected Type is {3} and requested type is {4}.",
                        key, group, this.AppKey, unCastedResult.GetType().FullName, typeof(T).FullName));
                return true;
            }
            else if (!_throwsExceptionOnNotFound)
            {
                result = default(T);
                Logger.WarnFormat("Config Key {0} not found on Group {1} into Settings container {2}", key, group, this.AppKey);         
                return false;
            }
            else
                throw new KeyNotFoundException(String.Format("Config Key {0} not found on Group {1} into Settings container {2}", key, group, this.AppKey));            
        }

        private void InnerSet<T>(string group, string key, T value)
        {
            try
            {
                IDictionary<string, object> bagContainer;
                if (!_settingsContainer.TryGetValue(group, out bagContainer))
                    throw new KeyNotFoundException(String.Format("Group {0} not found into Settings container {2}", group, this.AppKey)); 
                if (bagContainer.ContainsKey(key))                
                    bagContainer[key] = value;                
                else                
                    bagContainer.Add(key, value);                            
            }
            catch (Exception ex)
            {
                Logger.WarnFormat("Error on set config Key {0} on Group {1} into Settings container {2}: {3}", key, group, this.AppKey, ex.Message);  
                if (_throwsExceptionOnNotFound)
                    throw ex;
            }
        }

        #endregion

        #region Quick Setting Accessors

        public bool ContainsKey(string group, string key)
        {
            return InnerContainsKey(group, key);
        }

        /// <summary>
        /// Obtiene, de la configuracion, un objeto del tipo <see cref="System.String"/>.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <returns>Objeto de tipo <see cref="System.String"/> solicitado.</returns>
        /// <exception cref="InvalidCastException">No se ha podido transformar el objeto al tipo <see cref="System.String"/>.</exception>
        /// <exception cref="KeyNotFoundException">No se ha encotnrado la llave.</exception>
        public string Get(string group, string key)
        {
            string result;
            if (InnerGet(group, key, out result))
                return result;
            else
                throw new PropertyNotFoundExcpetion(group, key);         
        }

        /// <summary>
        /// Obtiene, de la configuracion, un objeto del tipo <see cref="{T}"/>.        
        /// </summary>
        /// <typeparam name="T"><see cref="System.Type"/> del objeto de configuracion que se queire leer.</typeparam>
        /// <param name="group">Codigo de groupo al que pertenece el objeto.</param>
        /// <param name="key">Llave del objeto dentro del grupo.</param>
        /// <returns>Objeto de tipo <see cref="{T}"/> solictado.</returns>
        /// <exception cref="InvalidCastException">No se ha podido transformar el objeto al tipo <see cref="{T}"/> solicitado.</exception>
        /// <exception cref="KeyNotFoundException">No se ha encotnrado la llave.</exception>
        public T Get<T>(string group, string key)
        {
            T result;
            if (InnerGet<T>(group, key, out result))
                return result;
            else
                throw new PropertyNotFoundExcpetion(group, key);    
        }

        /// <summary>
        /// Intenta obtener, de la configuracion, un objeto del tipo <see cref="System.String"/>.
        /// Si no lo consigue se devuelve un valor por defecto y un resultado false.
        /// </summary>
        /// <param name="group">Codigo de groupo al que pertenece el objeto.</param>
        /// <param name="key">Llave del objeto dentro del grupo.</param>
        /// <param name="result"></param>
        /// <returns>Exito en la devolviendo el valor.</returns>
        /// <remarks>En caso de excepcion a la hora de buscar el valor, no se lanzaran, pero si se anotan en el log de la aplicacion.</remarks>
        public bool TryGet(string group, string key, out string result)
        {
            try
            {
                return InnerGet(group, key, out result);
            }
            catch
            {
                result = default(string);
                return false;
            }            
        }

        /// <summary>
        /// Intenta obtener, de la configuracion, un objeto del tipo <see cref="{T}"/>. 
        /// Si no lo consigue se devuelve un valor <see cref="{T}"/> por defecto y un resultado false.
        /// </summary>
        /// <typeparam name="T"><see cref="System.Type"/> del objeto de configuracion que se queire leer.</typeparam>
        /// <param name="group">Codigo de groupo al que pertenece el objeto.</param>
        /// <param name="key">Llave del objeto dentro del grupo.</param>
        /// <param name="result">Valor obtenido de la configuracion.</param>
        /// <returns>Exito en la devolviendo el valor.</returns>
        /// <remarks>En caso de excepcion a la hora de buscar el valor, no se lanzaran, pero si se anotan en el log de la aplicacion.</remarks>
        public bool TryGet<T>(string group, string key, out T result)
        {
            try
            {
                return InnerGet<T>(group, key, out result);
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        /// <summary>
        /// Intenta establecer un valor en la configuracion. En caso de existir se sobreescribe el valor y el tipo.
        /// </summary>
        /// <typeparam name="T"><see cref="System.Type"/> del objeto de configuracion que se queire escribir.</typeparam>
        /// <param name="group">Codigo de groupo al que pertenece el objeto.</param>
        /// <param name="key">Llave del objeto dentro del grupo.</param>
        /// <param name="value">Valor a establecer.</param>
        /// <exception cref="KeyNotFoundException">No se ha encotnrado la llave del grupo.</exception>
        /// <remarks>
        /// Como el almacen es un <see cref="ConcurrentDictionary{String, Object}"/> el tipo del valor de puede modificar tambien. 
        /// Hay que tenerlo para evitar posibles <see cref="InvalidCastException"/>.
        /// </remarks>
        public void Set<T>(string group, string key, T value)
        {
            InnerSet<T>(group, key, value);
        }        

        #endregion

    }
}

