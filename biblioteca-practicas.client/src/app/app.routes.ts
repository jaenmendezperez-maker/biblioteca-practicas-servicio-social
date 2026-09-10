import { Routes } from '@angular/router';
import { ListaLibrosComponent } from './features/libros/lista-libros/lista-libros';
import { ListaCategoriasComponent } from './features/categorias/lista-categorias/lista-categorias';
import { LoginComponent } from './features/auth/login/login';
import { SubirLibroComponent } from './features/libros/subir-libro/subir-libro';
import { DetalleLibroComponent } from './features/libros/detalle-libro/detalle-libro';
import { PanelAdminComponent } from './features/admin/panel-admin/panel-admin';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { RegistroComponent } from './features/auth/registro/registro';
import { MisFavoritosComponent } from './features/favoritos/mis-favoritos/mis-favoritos';

export const routes: Routes = [
  { path: '', component: ListaLibrosComponent },
  { path: 'todos-los-libros', component: ListaLibrosComponent, data: { ocultarHero: true } },
  { path: 'categorias', component: ListaCategoriasComponent },
  { path: 'favoritos', component: MisFavoritosComponent, canActivate: [authGuard]},
  { path: 'login', component: LoginComponent },
  { path: 'registro', component: RegistroComponent },
  { path: 'subir', component: SubirLibroComponent, canActivate: [adminGuard] },
  { path: 'libro/:id', component: DetalleLibroComponent },
  { path: 'admin', component: PanelAdminComponent, canActivate: [adminGuard] },
];