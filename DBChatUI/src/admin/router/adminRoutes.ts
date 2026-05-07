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
        path: 'database/:database/:entity',
        component: DatabaseSectionPage,
        props: true
      }
    ]
  }
]
