import AdminLayout from '../components/AdminLayout.vue'
import EntityPage from '../pages/EntityPage.vue'
import DatabaseSectionPage from '../pages/DatabaseSectionPage.vue'

export default [

  {
    path: '/admin',
    component: AdminLayout,

    children: [

      {
        path: 'databases',
        component: EntityPage,
        props: {
          entity: 'databases'
        }
      },

      {
        path: 'llms',
        component: EntityPage,
        props: {
          entity: 'llms'
        }
      },

      {
        path: 'whatsappprofiles',
        component: EntityPage,
        props: {
          entity: 'whatsappprofiles'
        }
      },

      {
        path: 'users',
        component: EntityPage,
        props: {
          entity: 'users'
        }
      },

      {
        path: 'defaultsettings',
        component: EntityPage,
        props: {
          entity: 'defaultsettings'
        }
      },

      {
        path: 'defaultfolders',
        component: EntityPage,
        props: {
          entity: 'defaultfolders'
        }
      },

      {
        path: 'database/:database/schemacurator',
        component: () => import('@/components/Schema/SchemaCurator.vue'),
        props: true
      },

      {
        path: 'database/:database/:entity',
        component: DatabaseSectionPage,
        props: true
      }
    ]
  }
]
