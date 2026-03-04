export const buildRoute = (...segments: string[]) => segments.filter(Boolean).join('/');

export const AppRoutes = {
  Login: 'login',
  AdminRoot: 'admin',
  ClientRoot: 'client',
  Dashboard: 'dashboard',
  Analysis: 'analysis',
  Finance: 'finance',
  Users: 'users',
  Add: 'add',
  Edit: 'edit',
  Menu : "menu"
} as const;

export const AdminPaths = {
  Dashboard: buildRoute(AppRoutes.AdminRoot, AppRoutes.Dashboard),
  Analysis: buildRoute(AppRoutes.AdminRoot, AppRoutes.Analysis),
  UsersList: buildRoute(AppRoutes.AdminRoot, AppRoutes.Users),
  UserAdd: buildRoute(AppRoutes.AdminRoot, AppRoutes.Users, AppRoutes.Add),
  UserEdit: (id: string) => buildRoute(AppRoutes.AdminRoot, AppRoutes.Users, AppRoutes.Edit, id)
} as const;

export const UserPaths = {
  Dashboard: buildRoute(AppRoutes.ClientRoot, AppRoutes.Dashboard),
  Finance: buildRoute(AppRoutes.ClientRoot, AppRoutes.Finance),
  Profile : buildRoute(AppRoutes.ClientRoot, AppRoutes.Finance), // => vue mon compte du user
} as const;


export const AppAreas = {
  Admin: AppRoutes.AdminRoot,
  User: AppRoutes.ClientRoot,
} as const;