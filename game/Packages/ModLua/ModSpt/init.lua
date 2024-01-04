-- below can be called in any thread.
require('clrstruct.init')
require('libs.init')
-- below can be called only in UnityMain thread.
postinit('core.init', 200)
postinit('unity.init', 300)
-- require('config')
