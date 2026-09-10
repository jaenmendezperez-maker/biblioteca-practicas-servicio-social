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
import { ToastService } from '../../../core/services/toast.service';


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

  busquedaCategoria = '';
  busquedaLibro = '';
  busquedaUsuarios = '';

  nombreNuevaCategoria = '';
  categoriaEditandoId: number | null = null;
  nombreEditado = '';
  arrastrandoPdf = false;

  reprocesando = false;
  progresoReproceso = { actual: 0, total: 0 };

  paginaCategorias = 1;
  itemsPorPagina = 8;
  paginaUsuarios = 1;
  paginaLibros = 1;

  libroEditando: Libro | null = null;
  formEdicionLibro = {
    titulo: '',
    autor: '',
    descripcion: '',
    categoriaId: 0
  };
  nuevoArchivoPdf: File | null = null;
  guardandoEdicion = false;

  accionPendiente: AccionPendiente | null = null;

  constructor(
    private categoriaService: CategoriaService,
    private libroService: LibroService,
    private usuarioService: UsuarioService,
    public authService: AuthService,
    private reprocesamientoService: ReprocesamientoService,
    public sidebarService: SidebarService,
    private toast: ToastService
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
        this.toast.exito('Categoría creada correctamente.');
      },
      error: () => this.toast.error('No se pudo crear la categoría.')
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
        this.toast.exito('Categoría actualizada correctamente.');
      },
      error: () => this.toast.error('No se pudo editar la categoría.')
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
    this.nuevoArchivoPdf = null;
    this.arrastrandoPdf = false;
  }

  cancelarEdicionLibro(): void {
    this.libroEditando = null;
    this.nuevoArchivoPdf = null;
    this.arrastrandoPdf = false;
  }

  onArchivoPdfSeleccionado(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.nuevoArchivoPdf = input.files[0];
    }
  }

  guardarEdicionLibro(): void {
    if (!this.libroEditando) return;

    this.guardandoEdicion = true;

    this.libroService.editarLibro(this.libroEditando.id, this.formEdicionLibro).subscribe({
      next: () => {
        if (this.nuevoArchivoPdf && this.libroEditando) {
          this.libroService.reemplazarPdf(this.libroEditando.id, this.nuevoArchivoPdf).subscribe({
            next: () => {
              this.guardandoEdicion = false;
              this.cancelarEdicionLibro();
              this.cargarLibros();
              this.toast.exito('Libro actualizado y PDF reemplazado.');
            },
            error: () => {
              this.guardandoEdicion = false;
              this.error = 'Se guardaron los datos, pero no se pudo reemplazar el PDF.';
              this.cancelarEdicionLibro();
              this.cargarLibros();
              this.toast.error('Se guardaron los datos, pero no se pudo reemplazar el PDF.');
            }
          });
        } else {
          this.guardandoEdicion = false;
          this.cancelarEdicionLibro();
          this.cargarLibros();
          this.toast.exito('Libro actualizado correctamente.');
        }
      },
      error: () => {
        this.guardandoEdicion = false;
        this.toast.error('No se pudo editar el libro.');
      }
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
      next: () => {
        this.cargarUsuarios();
        this.toast.exito(`Rol de "${usuario.nombreCompleto}" actualizado.`);
      },
      error: () => this.toast.error('No se pudo cambiar el rol del usuario.')
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

  onDragOverEdicion(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.arrastrandoPdf = true;
  }

  onDragLeaveEdicion(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.arrastrandoPdf = false;
  }

  onArchivoPdfSoltado(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.arrastrandoPdf = false;

    if (!event.dataTransfer?.files || event.dataTransfer.files.length === 0) return;

    const file = event.dataTransfer.files[0];
    if (file.type !== 'application/pdf') {
      this.error = 'Solo se permiten archivos PDF.';
      return;
    }

    this.nuevoArchivoPdf = file;
  }

  // ===== Modal de confirmación =====

  cancelarAccion(): void {
    this.accionPendiente = null;
  }

  confirmarAccion(): void {
    if (!this.accionPendiente) return;

    const { tipo, id, nuevoEstado, nombre } = this.accionPendiente;
    const mensajeAccion = nuevoEstado ? 'reactivado' : 'dado de baja';

    if (tipo === 'categoria') {
      this.categoriaService.cambiarEstado(id, nuevoEstado).subscribe({
        next: () => {
          this.accionPendiente = null;
          this.cargarTodo();
          this.toast.exito(`"${nombre}" ha sido ${mensajeAccion}.`);
        },
        error: () => {
          this.toast.error('No se pudo cambiar el estado de la categoría.');
          this.accionPendiente = null;
        }
      });
    } else if (tipo === 'libro') {
      this.libroService.cambiarEstado(id, nuevoEstado).subscribe({
        next: () => {
          this.accionPendiente = null;
          this.cargarLibros();
          this.toast.exito(`"${nombre}" ha sido ${mensajeAccion}.`);
        },
        error: () => {
          this.toast.error('No se pudo cambiar el estado del libro.');
          this.accionPendiente = null;
        }
      });
    } else {
      this.usuarioService.cambiarEstado(id, nuevoEstado).subscribe({
        next: () => {
          this.accionPendiente = null;
          this.cargarUsuarios();
          this.toast.exito(`"${nombre}" ha sido ${mensajeAccion}.`);
        },
        error: () => {
          this.toast.error('No se pudo cambiar el estado del usuario.');
          this.accionPendiente = null;
        }
      });
    }
  }

  get categoriasFiltradas(): Categoria[] {
    if (!this.busquedaCategoria.trim()) return this.categorias;
    const termino = this.busquedaCategoria.toLowerCase();
    return this.categorias.filter((c) => c.nombre.toLowerCase().includes(termino));
  }

  get librosFiltrados(): Libro[] {
    if (!this.busquedaLibro.trim()) return this.libros;
    const termino = this.busquedaLibro.toLowerCase();
    return this.libros.filter((l) =>
      l.titulo.toLowerCase().includes(termino) ||
      (l.autor?.toLowerCase().includes(termino) ?? false) ||
      (l.categoriaNombre?.toLowerCase().includes(termino) ?? false)
    );
  }

  get usuariosFiltrados() {
    if (!this.busquedaUsuarios.trim()) return this.usuarios;
    const termino = this.busquedaUsuarios.toLowerCase();
    return this.usuarios.filter((u) =>
      u.nombreCompleto.toLowerCase().includes(termino) ||
      u.email.toLowerCase().includes(termino)
    );
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
  get categoriasPaginadas() {
    const inicio = (this.paginaCategorias - 1) * this.itemsPorPagina;
    return this.categoriasFiltradas.slice(inicio, inicio + this.itemsPorPagina);
  }

  get totalPaginasCategorias(): number {
    return Math.ceil(this.categoriasFiltradas.length / this.itemsPorPagina);
  }

  get usuariosPaginados() {
    const inicio = (this.paginaUsuarios - 1) * this.itemsPorPagina;
    return this.usuariosFiltrados.slice(inicio, inicio + this.itemsPorPagina);
  }

  get totalPaginasUsuarios(): number {
    return Math.ceil(this.usuariosFiltrados.length / this.itemsPorPagina);
  }

  get librosPaginadosAdmin() {
    const inicio = (this.paginaLibros - 1) * this.itemsPorPagina;
    return this.librosFiltrados.slice(inicio, inicio + this.itemsPorPagina);
  }

  get totalPaginasLibros(): number {
    return Math.ceil(this.librosFiltrados.length / this.itemsPorPagina);
  }
}