import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Categoria } from '../../../core/models/categoria.model';
import { Libro } from '../../../core/models/libro.model';
import { Usuario } from '../../../core/models/usuario.model';
import { CategoriaService } from '../../../core/services/categoria.service';
import { LibroService } from '../../../core/services/libro.service';
import { UsuarioService } from '../../../core/services/usuario.service';
import { AuthService } from '../../../core/services/auth.service';
import { SidebarComponent } from '../../../shared/components/sidebar/sidebar.component';
import { SidebarService } from '../../../core/services/sidebar.service';
import { ReprocesamientoService } from '../../../core/services/reprocesamiento.service';



interface AccionPendiente {
  tipo: 'categoria' | 'libro' | 'usuario';
  id: number;
  nombre: string;
  nuevoEstado: boolean;
}

const NOMBRES_ROL: Record<number, string> = {
  0: 'Admin',
  1: 'Docente',
  2: 'Alumno'
};

@Component({
  selector: 'app-panel-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent],
  templateUrl: './panel-admin.html'
})
export class PanelAdminComponent implements OnInit {
  tabActiva: 'categorias' | 'libros' | 'usuarios' = 'categorias';

  categorias: Categoria[] = [];
  libros: Libro[] = [];
  usuarios: Usuario[] = [];
  cargando = true;
  error = '';

  nombreNuevaCategoria = '';
  categoriaEditandoId: number | null = null;
  nombreEditado = '';

  reprocesando = false;
  progresoReproceso = { actual: 0, total: 0 };

  libroEditando: Libro | null = null;
  formEdicionLibro = {
    titulo: '',
    autor: '',
    descripcion: '',
    categoriaId: 0
  };

  accionPendiente: AccionPendiente | null = null;

  constructor(
    private categoriaService: CategoriaService,
    private libroService: LibroService,
    private usuarioService: UsuarioService,
    public authService: AuthService,
    private reprocesamientoService: ReprocesamientoService,
    public sidebarService: SidebarService
  ) {}

  ngOnInit(): void {
    this.cargarTodo();
  }

  cargarTodo(): void {
    this.cargando = true;
    this.categoriaService.getTodasLasCategorias().subscribe({
      next: (data) => {
        this.categorias = data;
        this.cargarLibros();
      },
      error: () => {
        this.error = 'No se pudieron cargar las categorías.';
        this.cargando = false;
      }
    });
  }

  cargarLibros(): void {
    this.libroService.getTodosLosLibros().subscribe({
      next: (data) => {
        this.libros = data;
        this.cargarUsuarios();
      },
      error: () => {
        this.error = 'No se pudieron cargar los libros.';
        this.cargando = false;
      }
    });
  }

  cargarUsuarios(): void {
    this.usuarioService.getUsuarios().subscribe({
      next: (data) => {
        this.usuarios = data;
        this.cargando = false;
      },
      error: () => {
        this.error = 'No se pudieron cargar los usuarios.';
        this.cargando = false;
      }
    });
  }

  cambiarTab(tab: 'categorias' | 'libros' | 'usuarios'): void {
    this.tabActiva = tab;
  }

  nombreRol(rol: number | string): string {
    if (typeof rol === 'string') return rol;
    return NOMBRES_ROL[rol] ?? 'Desconocido';
  }

  // ===== Categorías =====

  crearCategoria(): void {
    if (!this.nombreNuevaCategoria.trim()) return;

    this.categoriaService.crearCategoria(this.nombreNuevaCategoria).subscribe({
      next: () => {
        this.nombreNuevaCategoria = '';
        this.cargarTodo();
      },
      error: () => (this.error = 'No se pudo crear la categoría.')
    });
  }

  iniciarEdicionCategoria(cat: Categoria): void {
    this.categoriaEditandoId = cat.id;
    this.nombreEditado = cat.nombre;
  }

  cancelarEdicionCategoria(): void {
    this.categoriaEditandoId = null;
    this.nombreEditado = '';
  }

  guardarEdicionCategoria(cat: Categoria): void {
    if (!this.nombreEditado.trim()) return;

    this.categoriaService.editarCategoria(cat.id, this.nombreEditado).subscribe({
      next: () => {
        this.cancelarEdicionCategoria();
        this.cargarTodo();
      },
      error: () => (this.error = 'No se pudo editar la categoría.')
    });
  }

  pedirConfirmacionCategoria(cat: Categoria): void {
    this.accionPendiente = {
      tipo: 'categoria',
      id: cat.id,
      nombre: cat.nombre,
      nuevoEstado: !cat.activo
    };
  }

  // ===== Libros =====

  iniciarEdicionLibro(libro: Libro): void {
    this.libroEditando = libro;
    this.formEdicionLibro = {
      titulo: libro.titulo,
      autor: libro.autor || '',
      descripcion: libro.descripcion || '',
      categoriaId: libro.categoriaId
    };
  }

  cancelarEdicionLibro(): void {
    this.libroEditando = null;
  }

  guardarEdicionLibro(): void {
    if (!this.libroEditando) return;

    this.libroService.editarLibro(this.libroEditando.id, this.formEdicionLibro).subscribe({
      next: () => {
        this.cancelarEdicionLibro();
        this.cargarLibros();
      },
      error: () => (this.error = 'No se pudo editar el libro.')
    });
  }

  pedirConfirmacionLibro(libro: Libro): void {
    this.accionPendiente = {
      tipo: 'libro',
      id: libro.id,
      nombre: libro.titulo,
      nuevoEstado: !libro.activo
    };
  }

  // ===== Usuarios =====

  cambiarRolUsuario(usuario: Usuario, nuevoRol: string): void {
    const rolNumero = Number(nuevoRol);

    this.usuarioService.cambiarRol(usuario.id, rolNumero).subscribe({
      next: () => this.cargarUsuarios(),
      error: () => (this.error = 'No se pudo cambiar el rol del usuario.')
    });
  }

  pedirConfirmacionUsuario(usuario: Usuario): void {
    this.accionPendiente = {
      tipo: 'usuario',
      id: usuario.id,
      nombre: usuario.nombreCompleto,
      nuevoEstado: !usuario.activo
    };
  }

  esUsuarioActual(usuario: Usuario): boolean {
    return this.authService.usuarioActual()?.email === usuario.email;
  }

  // ===== Modal de confirmación =====

  cancelarAccion(): void {
    this.accionPendiente = null;
  }

  confirmarAccion(): void {
    if (!this.accionPendiente) return;

    const { tipo, id, nuevoEstado } = this.accionPendiente;

    if (tipo === 'categoria') {
      this.categoriaService.cambiarEstado(id, nuevoEstado).subscribe({
        next: () => {
          this.accionPendiente = null;
          this.cargarTodo();
        },
        error: () => {
          this.error = 'No se pudo cambiar el estado de la categoría.';
          this.accionPendiente = null;
        }
      });
    } else if (tipo === 'libro') {
      this.libroService.cambiarEstado(id, nuevoEstado).subscribe({
        next: () => {
          this.accionPendiente = null;
          this.cargarLibros();
        },
        error: () => {
          this.error = 'No se pudo cambiar el estado del libro.';
          this.accionPendiente = null;
        }
      });
    } else {
      this.usuarioService.cambiarEstado(id, nuevoEstado).subscribe({
        next: () => {
          this.accionPendiente = null;
          this.cargarUsuarios();
        },
        error: () => {
          this.error = 'No se pudo cambiar el estado del usuario.';
          this.accionPendiente = null;
        }
      });
    }
  }

  async reprocesarLibrosAntiguos(): Promise<void> {
    const librosAReprocesar = this.libros.filter((l) =>
      this.reprocesamientoService.necesitaReprocesar(l)
    );

    if (librosAReprocesar.length === 0) {
      alert('Todos los libros ya tienen sus metadatos completos.');
      return;
    }

    if (!confirm(`Se reprocesarán ${librosAReprocesar.length} libro(s). Esto puede tardar unos minutos. ¿Continuar?`)) {
      return;
    }

    this.reprocesando = true;
    this.progresoReproceso = { actual: 0, total: librosAReprocesar.length };

    for (const libro of librosAReprocesar) {
      try {
        await this.conTimeout(this.reprocesamientoService.reprocesarLibro(libro), 30000);
      } catch (err) {
        console.error(`Error o timeout al reprocesar el libro ${libro.titulo}`, err);
      }
      this.progresoReproceso.actual++;
    }

    this.reprocesando = false;
    this.cargarLibros();
  }

  private conTimeout<T>(promesa: Promise<T>, ms: number): Promise<T> {
    return Promise.race([
      promesa,
      new Promise<T>((_, reject) =>
        setTimeout(() => reject(new Error('Timeout: el libro tardó demasiado')), ms)
      )
    ]);
  }
}